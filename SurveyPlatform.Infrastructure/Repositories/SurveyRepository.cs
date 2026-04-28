using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Core.Interfaces;
using SurveyPlatform.Infrastructure;

namespace SurveyPlatform.Infrastructure.Repositories;

public class SurveyRepository : ISurveyRepository
{
    private readonly SurveyDbContext _context;

    public SurveyRepository(SurveyDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Survey>> GetActiveSurveysAsync(DateTime currentTime)
    {
        return await _context.Surveys
            .Include(s => s.Questions)
            .Where(s => s.IsActive && (s.ExpiresAt == DateTime.MinValue || s.ExpiresAt > currentTime))
            .ToListAsync();
    }

    public async Task<Survey?> GetSurveyWithDetailsAsync(Guid id)
    {
        return await _context.Surveys
            .Include(s => s.Questions.OrderBy(q => q.Order))
            .ThenInclude(q => q.Options.OrderBy(o => o.Order))
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Survey?> GetSurveyByIdAsync(Guid id)
    {
        return await _context.Surveys.FindAsync(id);
    }

    public async Task AddSurveyAsync(Survey survey)
    {
        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSurveyAsync(Survey survey)
    {
        _context.Surveys.Update(survey);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SurveyExistsAsync(Guid id)
    {
        return await _context.Surveys.AnyAsync(s => s.Id == id);
    }
}