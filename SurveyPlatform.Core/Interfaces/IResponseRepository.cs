namespace SurveyPlatform.Core.Interfaces;
using SurveyPlatform.Core.Entities;

public interface IResponseRepository
{
    Task<bool> HasUserRespondedAsync(Guid surveyId, string email);
    Task AddResponseAsync(Response response);
    Task<int> GetTotalResponsesCountAsync(Guid surveyId);
    Task<IEnumerable<Answer>> GetAnswersForSurveyResultsAsync(Guid surveyId);
    Task<IEnumerable<Response>> GetResponsesWithAnswersAsync(Guid surveyId);
}