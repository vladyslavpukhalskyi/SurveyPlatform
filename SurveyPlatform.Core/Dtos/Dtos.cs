namespace SurveyPlatform.Core.DTOs; // Або SurveyPlatform.Api.DTOs

public record UpdateSurveyDto(string Title, string Description, DateTime? ExpiresAt);
public record ActivateSurveyDto(bool IsActive);