using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Infrastructure;

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

    // GET: api/surveys
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Survey>>> GetActiveSurveys()
    {
        // Повертаємо лише активні опитування, термін яких не вичерпано
        return await _context.Surveys
            .Include(s => s.Questions)
            .Where(s => s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow))
            .ToListAsync();
    }

    // POST: api/surveys
    [HttpPost]
    public async Task<ActionResult<Survey>> CreateSurvey(Survey survey)
    {
        if (survey == null) return BadRequest();

        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetActiveSurveys), new { id = survey.Id }, survey);
    }

    // GET: api/surveys/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Survey>> GetSurvey(Guid id)
    {
        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null) return NotFound();

        return survey;
    }
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] Response responseRequest)
    {
        // 1. Шукаємо опитування
        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null) return NotFound("Опитування не знайдено.");

        // 2. Перевірка: чи активне воно?
        if (!survey.IsActive || survey.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Це опитування вже неактивне або завершене.");
        }

        // 3. Перевірка: чи цей email вже відповідав?
        var alreadyResponded = await _context.Responses
            .AnyAsync(r => r.SurveyId == id && r.RespondentEmail == responseRequest.RespondentEmail);
    
        if (alreadyResponded)
        {
            return BadRequest("Ви вже брали участь у цьому опитуванні.");
        }

        // 4. Валідація відповідей (Answers)
        foreach (var question in survey.Questions)
        {
            var answer = responseRequest.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

            // Перевірка на обов'язковість
            if (question.IsRequired && (answer == null || string.IsNullOrWhiteSpace(answer.Value)))
            {
                return BadRequest($"Питання '{question.Text}' є обов'язковим.");
            }

            // Перевірка Rating (якщо тип питання - Rating)
            if (question.Type == QuestionType.Rating && answer != null)
            {
                if (!int.TryParse(answer.Value, out int rating) || rating < 1 || rating > 5)
                {
                    return BadRequest("Оцінка повинна бути від 1 до 5.");
                }
            }
        }

        // 5. Збереження
        responseRequest.SurveyId = id;
        responseRequest.SubmittedAt = DateTime.UtcNow;
    
        _context.Responses.Add(responseRequest);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Дякуємо! Ваша відповідь збережена." });
    }
}