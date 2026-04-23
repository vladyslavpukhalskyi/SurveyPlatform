namespace SurveyPlatform.Core.Entities;

public class Option
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }

    public Question? Question { get; set; }
}