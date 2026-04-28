using SurveyPlatform.Core.Entities;

namespace SurveyPlatform.Core.Interfaces;

public interface ISurveyRepository
{
    Task<IEnumerable<Survey>> GetActiveSurveysAsync(DateTime currentTime);
    Task<Survey?> GetSurveyWithDetailsAsync(Guid id);
    Task<Survey?> GetSurveyByIdAsync(Guid id);
    Task AddSurveyAsync(Survey survey);
    Task UpdateSurveyAsync(Survey survey);
    Task<bool> SurveyExistsAsync(Guid id);
}