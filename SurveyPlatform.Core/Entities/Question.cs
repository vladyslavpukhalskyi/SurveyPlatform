namespace SurveyPlatform.Core.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    // Зв'язки
    public Survey? Survey { get; set; }
    public List<Option> Options { get; set; } = new();
}