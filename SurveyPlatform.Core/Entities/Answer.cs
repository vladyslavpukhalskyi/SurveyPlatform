namespace SurveyPlatform.Core.Entities;

public class Answer
{
    public Guid Id { get; set; }
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    public string Value { get; set; } = string.Empty; // Сюди записуємо текст, ID опції або число (Rating)

    public Response? Response { get; set; }
    public Question? Question { get; set; }
}   