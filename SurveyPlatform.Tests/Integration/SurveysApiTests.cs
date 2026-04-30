using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.DTOs;
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
        
        surveys.Should().NotBeNull();
        surveys!.Count.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public async Task Database_ShouldContainAtLeast10000Records()
    {
        var totalAnswers = await DbContext.Answers.CountAsync();
        totalAnswers.Should().BeGreaterThanOrEqualTo(10000);
    }

    [Fact]
    public async Task Respond_WithValidData_ShouldReturnOk()
    {
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        
        var testSurvey = new Survey
        {
            Id = surveyId,
            Title = "Isolated Test Survey",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(10),
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

        var response = await Client.PostAsJsonAsync($"/api/surveys/{surveyId}/respond", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetResults_ShouldReturnAggregatedData_WhenSurveyExists()
    {
        // Arrange
        var responseInDb = await DbContext.Responses.FirstAsync();
        var surveyId = responseInDb.SurveyId;

        // Act
        var response = await Client.GetAsync($"/api/surveys/{surveyId}/results");

        // Assert
        response.EnsureSuccessStatusCode(); 
        
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("totalResponses");
        content.Should().Contain("questionsResults");
    }

    [Fact]
    public async Task ExportResults_ShouldReturnJsonFile_WhenSurveyExists()
    {
        // Arrange
        var responseInDb = await DbContext.Responses.FirstAsync();
        var surveyId = responseInDb.SurveyId;

        // Act
        var response = await Client.GetAsync($"/api/surveys/{surveyId}/export");

        // Assert
        response.EnsureSuccessStatusCode();
        
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentDisposition!.FileName.Should().Contain("export.json");

        var exportedData = await response.Content.ReadFromJsonAsync<List<Response>>();
        exportedData.Should().NotBeNull();
        exportedData!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSurvey_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var newSurvey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Нове опитування через API",
            Description = "Перевіряємо ендпоінт створення",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Questions = new List<Question>
            {
                new() 
                { 
                    Id = Guid.NewGuid(), 
                    Text = "Як вам наш API?", 
                    Type = QuestionType.Rating, 
                    IsRequired = true, 
                    Order = 1 
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/surveys", newSurvey);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdSurvey = await response.Content.ReadFromJsonAsync<Survey>();
        createdSurvey.Should().NotBeNull();
        createdSurvey!.Title.Should().Be(newSurvey.Title);
        createdSurvey.Questions.Should().HaveCount(1);
    }
    [Fact]
    public async Task ActivateDeactivateSurvey_ShouldChangeStatus_WhenSurveyExists()
    {
        // Arrange
        var newSurvey = new Survey 
        { 
            Title = "Test Patch Survey", 
            IsActive = false 
        };
    
        var createResponse = await Client.PostAsJsonAsync("/api/surveys", newSurvey);
        var createdSurvey = await createResponse.Content.ReadFromJsonAsync<Survey>();
        var surveyId = createdSurvey!.Id;

        var patchRequest = new ActivateSurveyDto(true); 

        // Act
        var patchResponse = await Client.PatchAsJsonAsync($"/api/surveys/{surveyId}/activate", patchRequest);

        // Assert
        patchResponse.EnsureSuccessStatusCode();
        
        var getResponse = await Client.GetAsync($"/api/surveys/{surveyId}");
        var updatedSurvey = await getResponse.Content.ReadFromJsonAsync<Survey>();
    
        updatedSurvey!.IsActive.Should().BeTrue();
    }
}