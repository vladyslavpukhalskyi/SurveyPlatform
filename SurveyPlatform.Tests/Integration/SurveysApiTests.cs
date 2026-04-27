using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Tests.Abstractions;
using Xunit;

namespace SurveyPlatform.Tests.Integration;

public class SurveysApiTests : BaseIntegrationTest
{
    [Fact]
    public async Task GetSurveys_ShouldReturnSeededData()
    {
        var response = await Client.GetAsync("/api/surveys");
        response.EnsureSuccessStatusCode();
        var surveys = await response.Content.ReadFromJsonAsync<List<Survey>>();
        
        surveys.Should().NotBeEmpty();
        surveys.Count.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public async Task CreateSurvey_ShouldReturnCreated_AndSaveToDatabase()
    {
        // Arrange
        var newSurvey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Integration Test Survey",
            Description = "Testing API",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Questions = new List<Question>
            {
                new() { Id = Guid.NewGuid(), Text = "Q1", Type = QuestionType.Text, Order = 1 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/surveys", newSurvey);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSurvey = await response.Content.ReadFromJsonAsync<Survey>();
        createdSurvey.Should().NotBeNull();
        createdSurvey!.Title.Should().Be("Integration Test Survey");

        // Перевіряємо, чи дійсно збереглося в БД
        var existsInDb = await DbContext.Surveys.AnyAsync(s => s.Id == createdSurvey.Id);
        existsInDb.Should().BeTrue();
    }

    [Fact]
    public async Task Respond_WithValidData_ShouldReturnOk()
    {
        // Arrange: Створюємо власне опитування для тесту з рівно 1 обов'язковим питанням
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        
        var testSurvey = new Survey
        {
            Id = surveyId,
            Title = "Bulletproof Test Survey",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(10), // Точно активне і не прострочене
            Questions = new List<Question>
            {
                new() { Id = questionId, Text = "Test Question", Type = QuestionType.Text, IsRequired = true, Order = 1 }
            }
        };

        DbContext.Surveys.Add(testSurvey);
        await DbContext.SaveChangesAsync();
        
        var payload = new Response
        {
            RespondentEmail = $"new_respondent_{Guid.NewGuid()}@test.com",
            Answers = new List<Answer>
            {
                new() { QuestionId = questionId, Value = "Test Answer" }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/surveys/{surveyId}/respond", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetResults_ShouldReturnAggregatedData()
    {
        // Arrange
        var survey = await DbContext.Surveys.FirstAsync();

        // Act
        var response = await Client.GetAsync($"/api/surveys/{survey.Id}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Очікуємо анонімний об'єкт або JsonDocument, оскільки DTO не вказано в умові
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalResponses");
        content.Should().Contain("questionsResults");
    }
}