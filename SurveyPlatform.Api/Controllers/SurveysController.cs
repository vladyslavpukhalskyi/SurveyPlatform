using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    // PUT: api/surveys/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSurvey(Guid id, [FromBody] Survey updatedSurvey)
    {
        if (id != updatedSurvey.Id) 
            return BadRequest("ID у маршруті не співпадає з ID у тілі запиту.");

        var existingSurvey = await _context.Surveys.FindAsync(id);
        if (existingSurvey == null) 
            return NotFound("Опитування не знайдено.");

        existingSurvey.Title = updatedSurvey.Title;
        existingSurvey.Description = updatedSurvey.Description;
        existingSurvey.ExpiresAt = updatedSurvey.ExpiresAt;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PATCH: api/surveys/{id}/activate
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> ToggleActivation(Guid id, [FromBody] bool isActive)
    {
        var survey = await _context.Surveys.FindAsync(id);
        if (survey == null) 
            return NotFound("Опитування не знайдено.");

        survey.IsActive = isActive;
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    // POST: api/surveys/{id}/respond
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] Response responseRequest)
    {
        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null) return NotFound("Опитування не знайдено.");

        // ПЕРЕВІРКА НА ЗАВЕРШЕННЯ ОПИТУВАННЯ (Оновлено)
        if (survey.ExpiresAt != DateTime.MinValue && survey.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Опитування неактивне або завершене.");
        }

        var alreadyResponded = await _context.Responses
            .AnyAsync(r => r.SurveyId == id && r.RespondentEmail == responseRequest.RespondentEmail);
    
        if (alreadyResponded)
        {
            return BadRequest("Ви вже брали участь у цьому опитуванні.");
        }

        foreach (var question in survey.Questions)
        {
            var answer = responseRequest.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

            if (question.IsRequired && (answer == null || string.IsNullOrWhiteSpace(answer.Value)))
            {
                return BadRequest($"Питання '{question.Text}' є обов'язковим.");
            }

            if (question.Type == QuestionType.Rating && answer != null)
            {
                if (!int.TryParse(answer.Value, out int rating) || rating < 1 || rating > 5)
                {
                    return BadRequest("Оцінка повинна бути від 1 до 5.");
                }
            }
        }

        responseRequest.SurveyId = id;
        responseRequest.SubmittedAt = DateTime.UtcNow;
    
        _context.Responses.Add(responseRequest);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Дякуємо! Ваша відповідь збережена." });
    }

    // GET: api/surveys/{id}/results
    [HttpGet("{id}/results")]
    public async Task<IActionResult> GetResults(Guid id)
    {
        var surveyExists = await _context.Surveys.AnyAsync(s => s.Id == id);
        if (!surveyExists) 
            return NotFound("Опитування не знайдено.");

        var totalResponses = await _context.Responses.CountAsync(r => r.SurveyId == id);

        var results = await _context.Answers
            .Include(a => a.Question)
            .Where(a => a.Question.SurveyId == id)
            .GroupBy(a => new { a.QuestionId, a.Question.Text, a.Question.Type })
            .Select(g => new
            {
                QuestionId = g.Key.QuestionId,
                QuestionText = g.Key.Text,
                QuestionType = g.Key.Type.ToString(),
                TotalAnswers = g.Count(),
                Breakdown = g.GroupBy(a => a.Value)
                             .Select(vg => new { Value = vg.Key, Count = vg.Count() }),
                AverageRating = g.Key.Type == QuestionType.Rating 
                                ? g.Average(a => Convert.ToDouble(a.Value)) 
                                : (double?)null
            })
            .ToListAsync();

        return Ok(new
        {
            SurveyId = id,
            TotalResponses = totalResponses,
            QuestionsResults = results
        });
    }

    // GET: api/surveys/{id}/export
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportResponses(Guid id)
    {
        var surveyExists = await _context.Surveys.AnyAsync(s => s.Id == id);
        if (!surveyExists) 
            return NotFound("Опитування не знайдено.");

        var responses = await _context.Responses
            .Include(r => r.Answers)
            .Where(r => r.SurveyId == id)
            .ToListAsync();

        if (!responses.Any()) 
            return BadRequest("Немає відповідей для експорту.");

        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        var jsonString = JsonSerializer.Serialize(responses, jsonOptions);
        var bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

        return File(bytes, "application/json", $"survey_{id}_responses.json");
    }
}