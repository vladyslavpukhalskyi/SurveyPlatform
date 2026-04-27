using System.Text.Json.Serialization; // Обов'язково додай цей using

namespace SurveyPlatform.Core.Entities;

public class Response
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string RespondentEmail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }

    public List<Answer> Answers { get; set; } = new();

    // РОБИМО ВЛАСТИВІСТЬ ОПЦІОНАЛЬНОЮ ДЛЯ JSON ВАЛІДАТОРА
    [JsonIgnore]
    public Survey? Survey { get; set; }
}