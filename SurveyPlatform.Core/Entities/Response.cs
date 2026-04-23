namespace SurveyPlatform.Core.Entities;

public class Response
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string RespondentEmail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Survey? Survey { get; set; }
    public List<Answer> Answers { get; set; } = new();
}