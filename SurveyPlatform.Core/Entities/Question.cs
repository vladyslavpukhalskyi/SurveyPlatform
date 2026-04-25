using SurveyPlatform.Core.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    // Додай ці рядки:
    public List<Option> Options { get; set; } = new();
    public List<Answer> Answers { get; set; } = new();
}