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
}