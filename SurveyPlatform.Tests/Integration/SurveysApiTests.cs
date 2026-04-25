using System.Net.Http.Json;
using FluentAssertions;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Tests.Abstractions;
using Xunit;

namespace SurveyPlatform.Tests.Integration;

public class SurveysApiTests : BaseIntegrationTest
{
    [Fact]
    public async Task GetSurveys_ShouldReturnSeededData()
    {
        // Act: надсилаємо запит до API
        var response = await Client.GetAsync("/api/surveys");

        // Assert: перевіряємо, чи все пройшло успішно
        response.EnsureSuccessStatusCode();
        var surveys = await response.Content.ReadFromJsonAsync<List<Survey>>();
        
        surveys.Should().NotBeEmpty();
        // Перевіряємо, що повернулося саме 100 опитувань, які ми згенерували
        surveys.Count.Should().Be(100); 
    }
}