using SurveyPlatform.Core.Entities;

public class Response
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string RespondentEmail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }

    // Додай цей рядок:
    public List<Answer> Answers { get; set; } = new();
}