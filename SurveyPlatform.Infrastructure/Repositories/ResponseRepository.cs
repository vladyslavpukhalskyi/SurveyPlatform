using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Core.Interfaces;
using SurveyPlatform.Infrastructure;

namespace SurveyPlatform.Infrastructure.Repositories;

public class ResponseRepository : IResponseRepository
{
    private readonly SurveyDbContext _context;

    public ResponseRepository(SurveyDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasUserRespondedAsync(Guid surveyId, string email)
    {
        return await _context.Responses
            .AnyAsync(r => r.SurveyId == surveyId && r.RespondentEmail == email);
    }

    public async Task AddResponseAsync(Response response)
    {
        _context.Responses.Add(response);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetTotalResponsesCountAsync(Guid surveyId)
    {
        return await _context.Responses.CountAsync(r => r.SurveyId == surveyId);
    }

    public async Task<IEnumerable<Answer>> GetAnswersForSurveyResultsAsync(Guid surveyId)
    {
        return await _context.Answers
            .Include(a => a.Question)
            .Where(a => a.Question.SurveyId == surveyId)
            .Select(a => new Answer 
            { 
                QuestionId = a.QuestionId, 
                Value = a.Value,
                Question = new Question { Text = a.Question.Text, Type = a.Question.Type } 
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<Response>> GetResponsesWithAnswersAsync(Guid surveyId)
    {
        return await _context.Responses
            .Include(r => r.Answers)
            .Where(r => r.SurveyId == surveyId)
            .ToListAsync();
    }
}