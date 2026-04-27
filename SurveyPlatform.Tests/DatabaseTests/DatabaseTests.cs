using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Tests.Abstractions;
using Xunit;

namespace SurveyPlatform.Tests.Integration;

public class DatabaseTests : BaseIntegrationTest
{
    [Fact]
    public async Task SubmitResponse_WithDuplicateEmailForSameSurvey_ShouldThrowDbUpdateException()
    {
        // Arrange
        var survey = await DbContext.Surveys.FirstAsync();
        var email = "duplicate@example.com";
        
        var response1 = new Response { Id = Guid.NewGuid(), SurveyId = survey.Id, RespondentEmail = email, SubmittedAt = DateTime.UtcNow };
        DbContext.Responses.Add(response1);
        await DbContext.SaveChangesAsync();

        // Act
        var response2 = new Response { Id = Guid.NewGuid(), SurveyId = survey.Id, RespondentEmail = email, SubmittedAt = DateTime.UtcNow };
        DbContext.Responses.Add(response2);

        // Assert
        Func<Task> act = async () => await DbContext.SaveChangesAsync();
        
        // Ми прибрали .WithMessage(...), щоб тест не падав через різницю у текстах помилок 
        // між різними драйверами баз даних. Головне, що Entity Framework кидає DbUpdateException.
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeleteSurvey_ShouldCascadeDeleteQuestionsAndResponses()
    {
        // Arrange
        var survey = await DbContext.Surveys.FirstAsync();
        var surveyId = survey.Id;

        // Act
        DbContext.Surveys.Remove(survey);
        await DbContext.SaveChangesAsync();

        // Assert
        var questionsExist = await DbContext.Questions.AnyAsync(q => q.SurveyId == surveyId);
        var responsesExist = await DbContext.Responses.AnyAsync(r => r.SurveyId == surveyId);

        questionsExist.Should().BeFalse("всі питання мають бути видалені каскадно");
        responsesExist.Should().BeFalse("всі відповіді мають бути видалені каскадно");
    }

    [Fact]
    public async Task Questions_ShouldMaintainOrder()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Title = "Order Test", IsActive = true };
        
        // Створюємо питання з перемішаним Order
        var q1 = new Question { Id = Guid.NewGuid(), SurveyId = surveyId, Text = "First", Order = 1, Type = QuestionType.Text };
        var q3 = new Question { Id = Guid.NewGuid(), SurveyId = surveyId, Text = "Third", Order = 3, Type = QuestionType.Text };
        var q2 = new Question { Id = Guid.NewGuid(), SurveyId = surveyId, Text = "Second", Order = 2, Type = QuestionType.Text };
        
        DbContext.Surveys.Add(survey);
        DbContext.Questions.AddRange(q3, q1, q2); // Додаємо у БД врозкид
        await DbContext.SaveChangesAsync();

        // Act
        var retrievedQuestions = await DbContext.Questions
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.Order)
            .ToListAsync();

        // Assert
        retrievedQuestions.Should().BeInAscendingOrder(q => q.Order);
        retrievedQuestions[0].Text.Should().Be("First");
        retrievedQuestions[1].Text.Should().Be("Second");
        retrievedQuestions[2].Text.Should().Be("Third");
    }
}