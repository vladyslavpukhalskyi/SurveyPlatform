using System.ComponentModel.DataAnnotations.Schema;

public class Option
{
    public Guid Id { get; set; }
    
    public Guid QuestionId { get; set; }
    
    [ForeignKey(nameof(QuestionId))] // Явно вказуємо зв'язок
    public Question? Question { get; set; }

    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}