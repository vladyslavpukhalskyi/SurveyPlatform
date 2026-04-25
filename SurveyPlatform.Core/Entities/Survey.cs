using SurveyPlatform.Core.Entities;

public class Survey
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Додай ці рядки:
    public List<Question> Questions { get; set; } = new();
    public List<Response> Responses { get; set; } = new();
}