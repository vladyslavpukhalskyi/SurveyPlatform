using System.Text.Json.Serialization;

namespace SurveyPlatform.Core.Entities;

public class Answer
{
    public Guid Id { get; set; }
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    public string Value { get; set; } = string.Empty;

    [JsonIgnore]
    public Response? Response { get; set; }

    [JsonIgnore]
    public Question? Question { get; set; }
}