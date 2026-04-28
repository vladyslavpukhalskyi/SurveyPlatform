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
        // Створюємо ізольоване опитування лише з ОДНИМ питанням, 
        // щоб обійти проблему "5 обов'язкових питань" із сідеру.
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
        
        // Відправляємо відповідь саме на це одне питання
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
        // Arrange: Знаходимо ID опитування, для якого ТОЧНО є хоча б одна відповідь у БД
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
        // Arrange: Знаходимо ID опитування, для якого ТОЧНО є хоча б одна відповідь у БД
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
        // Arrange: Готуємо payload для нового опитування
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

        // Act: Відправляємо POST запит
        var response = await Client.PostAsJsonAsync("/api/surveys", newSurvey);

        // Assert: Перевіряємо статус 201 Created та повернені дані
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdSurvey = await response.Content.ReadFromJsonAsync<Survey>();
        createdSurvey.Should().NotBeNull();
        createdSurvey!.Title.Should().Be(newSurvey.Title);
        createdSurvey.Questions.Should().HaveCount(1);
    }
}