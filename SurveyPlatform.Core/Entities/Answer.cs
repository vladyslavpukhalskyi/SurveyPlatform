using System.ComponentModel.DataAnnotations.Schema;

namespace SurveyPlatform.Core.Entities;

public class Answer
{
    public Guid Id { get; set; }
    
    public Guid ResponseId { get; set; }
    
    [ForeignKey(nameof(ResponseId))] // Явно вказуємо зв'язок
    public Response? Response { get; set; }

    public Guid QuestionId { get; set; }
    
    [ForeignKey(nameof(QuestionId))] // Явно вказуємо зв'язок
    public Question? Question { get; set; }

    public string Value { get; set; } = string.Empty;
}