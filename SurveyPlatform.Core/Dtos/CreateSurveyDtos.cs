namespace SurveyPlatform.Core.DTOs
{
    // DTO для створення опитування
    public class CreateSurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public List<CreateQuestionDto> Questions { get; set; } = new();
    }

    public class CreateQuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public int Type { get; set; } // Enum передаємо як число
        public bool IsRequired { get; set; }
        public int Order { get; set; }
        public List<CreateOptionDto>? Options { get; set; }
    }

    public class CreateOptionDto
    {
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
    }
    
    public class CreateResponseDto
    {
        public string RespondentEmail { get; set; } = string.Empty;
        public List<CreateAnswerDto> Answers { get; set; } = new();
    }

    public class CreateAnswerDto
    {
        public Guid QuestionId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}