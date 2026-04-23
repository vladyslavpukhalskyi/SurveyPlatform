namespace SurveyPlatform.Core.Entities;

public class Survey
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Навігаційна властивість для питань
    public List<Question> Questions { get; set; } = new();
}