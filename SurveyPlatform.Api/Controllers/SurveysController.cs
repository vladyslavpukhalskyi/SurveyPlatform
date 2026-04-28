using Microsoft.AspNetCore.Mvc;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Core.Interfaces;
using System.Text.Json;
using SurveyPlatform.Core.DTOs;

namespace SurveyPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IResponseRepository _responseRepo;

    public SurveysController(ISurveyRepository surveyRepo, IResponseRepository responseRepo)
    {
        _surveyRepo = surveyRepo;
        _responseRepo = responseRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Survey>>> GetActiveSurveys()
    {
        var surveys = await _surveyRepo.GetActiveSurveysAsync(DateTime.UtcNow);
        return Ok(surveys);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Survey>> GetSurvey(Guid id)
    {
        var survey = await _surveyRepo.GetSurveyWithDetailsAsync(id);
        if (survey == null) return NotFound("Опитування не знайдено.");

        return Ok(survey);
    }

    [HttpPost]
    public async Task<ActionResult<Survey>> CreateSurvey(Survey survey)
    {
        await _surveyRepo.AddSurveyAsync(survey);
        return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSurvey(Guid id, [FromBody] UpdateSurveyDto request)
    {
        var survey = await _surveyRepo.GetSurveyByIdAsync(id);
        if (survey == null) return NotFound("Опитування не знайдено.");

        survey.Title = request.Title;
        survey.Description = request.Description;
        if (request.ExpiresAt.HasValue)
        {
            survey.ExpiresAt = request.ExpiresAt.Value.ToUniversalTime();
        }

        await _surveyRepo.UpdateSurveyAsync(survey);
        return Ok(survey);
    }

    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] Response responseRequest)
    {
        try
        {
            var survey = await _surveyRepo.GetSurveyWithDetailsAsync(id);
            if (survey == null) return NotFound("Опитування не знайдено.");
            
            if (!survey.IsActive || (survey.ExpiresAt != DateTime.MinValue && survey.ExpiresAt < DateTime.UtcNow))
                return BadRequest("Опитування неактивне або завершене.");
            
            var alreadyResponded = await _responseRepo.HasUserRespondedAsync(id, responseRequest.RespondentEmail);
            if (alreadyResponded) return BadRequest("Ви вже брали участь.");

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

            if (responseRequest.Id == Guid.Empty) responseRequest.Id = Guid.NewGuid();
            responseRequest.SurveyId = id;
            responseRequest.SubmittedAt = DateTime.UtcNow;

            foreach (var ans in responseRequest.Answers)
            {
                if (ans.Id == Guid.Empty) ans.Id = Guid.NewGuid();
            }

            await _responseRepo.AddResponseAsync(responseRequest);
            return Ok(new { Message = "Відповідь збережена." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"КРИТИЧНА ПОМИЛКА: {ex.Message} | ДЕТАЛІ: {ex.InnerException?.Message}");
        }
    }

    [HttpGet("{id}/results")]
    public async Task<IActionResult> GetResults(Guid id)
    {
        var totalResponses = await _responseRepo.GetTotalResponsesCountAsync(id);
        var answers = await _responseRepo.GetAnswersForSurveyResultsAsync(id);

        var results = answers
            .GroupBy(a => new { a.QuestionId, a.Question!.Text, a.Question.Type })
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

    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportResults(Guid id)
    {
        var surveyExists = await _surveyRepo.SurveyExistsAsync(id);
        if (!surveyExists) return NotFound("Опитування не знайдено.");

        var responses = await _responseRepo.GetResponsesWithAnswersAsync(id);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(responses, jsonOptions);

        return File(jsonBytes, "application/json", $"survey_{id}_export.json");
    }
}