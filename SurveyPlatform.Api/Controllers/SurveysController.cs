using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Infrastructure;
using System.Text.Json;

namespace SurveyPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly SurveyDbContext _context;

    public SurveysController(SurveyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Survey>>> GetActiveSurveys()
    {
        var now = DateTime.UtcNow;
        return await _context.Surveys
            .Include(s => s.Questions)
            .Where(s => s.IsActive && (s.ExpiresAt == DateTime.MinValue || s.ExpiresAt > now))
            .ToListAsync();
    }

    // 1. ОТРИМАТИ ОДНЕ ОПИТУВАННЯ (З питаннями та опціями, відсортованими за Order)
    [HttpGet("{id}")]
    public async Task<ActionResult<Survey>> GetSurvey(Guid id)
    {
        var survey = await _context.Surveys
            .Include(s => s.Questions.OrderBy(q => q.Order))
            .ThenInclude(q => q.Options.OrderBy(o => o.Order))
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null) return NotFound("Опитування не знайдено.");

        return Ok(survey);
    }

    [HttpPost]
    public async Task<ActionResult<Survey>> CreateSurvey(Survey survey)
    {
        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
    }

    // 2. ОНОВИТИ ОПИТУВАННЯ (Назву, опис, дату завершення)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSurvey(Guid id, [FromBody] UpdateSurveyDto request)
    {
        var survey = await _context.Surveys.FindAsync(id);
        if (survey == null) return NotFound("Опитування не знайдено.");

        survey.Title = request.Title;
        survey.Description = request.Description;
        if (request.ExpiresAt.HasValue)
        {
            survey.ExpiresAt = request.ExpiresAt.Value.ToUniversalTime();
        }

        await _context.SaveChangesAsync();
        return Ok(survey);
    }

    // 3. ДОДАТИ ВІДПОВІДЬ (RESPOND)
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] Response responseRequest)
    {
        try
        {
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null) return NotFound("Опитування не знайдено.");
            
            if (!survey.IsActive || (survey.ExpiresAt != DateTime.MinValue && survey.ExpiresAt < DateTime.UtcNow))
                return BadRequest("Опитування неактивне або завершене.");
            
            var alreadyResponded = await _context.Responses
                .AnyAsync(r => r.SurveyId == id && r.RespondentEmail == responseRequest.RespondentEmail);
            
            if (alreadyResponded) return BadRequest("Ви вже брали участь.");

            // ЗАХИСТ ВІД NULL: Якщо масив відповідей порожній, створюємо пустий список
            responseRequest.Answers ??= new List<Answer>();

            foreach (var question in survey.Questions)
            {
                var answer = responseRequest.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

                if (question.IsRequired && (answer == null || string.IsNullOrWhiteSpace(answer.Value)))
                    return BadRequest($"Питання '{question.Text}' є обов'язковим.");

                if (answer != null && question.Type == QuestionType.Rating)
                {
                    if (!int.TryParse(answer.Value, out int r) || r < 1 || r > 5)
                        return BadRequest("Рейтинг має бути від 1 до 5.");
                }

                if (answer != null && question.Type == QuestionType.SingleChoice)
                {
                    if (!question.Options.Any(o => o.Text == answer.Value))
                        return BadRequest($"Невалідний варіант для: {question.Text}");
                }
            }

            // ГЕНЕРАЦІЯ ID: Явно створюємо нові Guid, щоб уникнути конфліктів у БД
            if (responseRequest.Id == Guid.Empty) responseRequest.Id = Guid.NewGuid();
            responseRequest.SurveyId = id;
            responseRequest.SubmittedAt = DateTime.UtcNow;

            foreach (var ans in responseRequest.Answers)
            {
                if (ans.Id == Guid.Empty) ans.Id = Guid.NewGuid();
            }

            _context.Responses.Add(responseRequest);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Відповідь збережена." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"КРИТИЧНА ПОМИЛКА: {ex.Message} | ДЕТАЛІ: {ex.InnerException?.Message}");
        }
    }

    // 4. ОТРИМАТИ РЕЗУЛЬТАТИ
    [HttpGet("{id}/results")]
    public async Task<IActionResult> GetResults(Guid id)
    {
        var totalResponses = await _context.Responses.CountAsync(r => r.SurveyId == id);

        // 1. Витягуємо лише необхідні поля з БД без спроб математичних конвертацій у SQL
        var answers = await _context.Answers
            .Include(a => a.Question)
            .Where(a => a.Question.SurveyId == id)
            .Select(a => new { a.QuestionId, a.Question.Text, a.Question.Type, a.Value })
            .ToListAsync();

        // 2. Робимо групування та розрахунок середнього значення в оперативній пам'яті
        var results = answers
            .GroupBy(a => new { a.QuestionId, a.Text, a.Type })
            .Select(g => new
            {
                QuestionText = g.Key.Text,
                TotalAnswers = g.Count(),
                AverageRating = g.Key.Type == QuestionType.Rating 
                    ? g.Where(a => double.TryParse(a.Value, out _)).Average(a => Convert.ToDouble(a.Value)) 
                    : (double?)null
            })
            .ToList();

        return Ok(new { SurveyId = id, TotalResponses = totalResponses, QuestionsResults = results });
    }

    // 5. ЕКСПОРТУВАТИ РЕЗУЛЬТАТИ
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportResults(Guid id)
    {
        var surveyExists = await _context.Surveys.AnyAsync(s => s.Id == id);
        if (!surveyExists) return NotFound("Опитування не знайдено.");

        var responses = await _context.Responses
            .Include(r => r.Answers)
            .Where(r => r.SurveyId == id)
            .ToListAsync();

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(responses, jsonOptions);

        return File(jsonBytes, "application/json", $"survey_{id}_export.json");
    }
}

// DTO класи для нових методів
public record UpdateSurveyDto(string Title, string Description, DateTime? ExpiresAt);
public record ActivateSurveyDto(bool IsActive);