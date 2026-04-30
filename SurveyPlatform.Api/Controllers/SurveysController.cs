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
    public async Task<ActionResult<Survey>> CreateSurvey([FromBody] CreateSurveyDto dto)
    {
        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            ExpiresAt = dto.ExpiresAt.ToUniversalTime(),
            Questions = dto.Questions.Select(q => new Question
            {
                Id = Guid.NewGuid(),
                Text = q.Text,
                Type = (QuestionType)q.Type,
                IsRequired = q.IsRequired,
                Order = q.Order,
                Options = q.Options?.Select(o => new Option
                {
                    Id = Guid.NewGuid(),
                    Text = o.Text,
                    Order = o.Order
                }).ToList() ?? new List<Option>()
            }).ToList()
        };

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
    
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> ActivateDeactivateSurvey(Guid id, [FromBody] ActivateSurveyDto request)
    {
        var survey = await _surveyRepo.GetSurveyByIdAsync(id);
        if (survey == null) return NotFound("Опитування не знайдено.");

        survey.IsActive = request.IsActive;
        
        await _surveyRepo.UpdateSurveyAsync(survey);

        return Ok(new 
        { 
            Message = $"Опитування {(request.IsActive ? "активовано" : "деактивовано")}.", 
            IsActive = survey.IsActive 
        });
    }
    
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] CreateResponseDto dto)
    {
        try
        {
            var survey = await _surveyRepo.GetSurveyWithDetailsAsync(id);
            if (survey == null) return NotFound("Опитування не знайдено.");
            
            if (!survey.IsActive || (survey.ExpiresAt != DateTime.MinValue && survey.ExpiresAt < DateTime.UtcNow))
                return BadRequest("Опитування неактивне або завершене.");
            
            var alreadyResponded = await _responseRepo.HasUserRespondedAsync(id, dto.RespondentEmail);
            if (alreadyResponded) return BadRequest("Ви вже брали участь.");

            var answersDto = dto.Answers ?? new List<CreateAnswerDto>();
            
            foreach (var question in survey.Questions)
            {
                var answer = answersDto.FirstOrDefault(a => a.QuestionId == question.Id);

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
            
            var responseEntity = new Response
            {
                Id = Guid.NewGuid(),
                SurveyId = id,
                RespondentEmail = dto.RespondentEmail,
                SubmittedAt = DateTime.UtcNow,
                Answers = answersDto.Select(a => new Answer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = a.QuestionId,
                    Value = a.Value
                }).ToList()
            };

            await _responseRepo.AddResponseAsync(responseEntity);
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