using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Api.Controllers;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Infrastructure;
using Xunit;

namespace SurveyPlatform.Tests.UnitTests;

public class ValidationTests
{
    private readonly SurveyDbContext _context;
    private readonly SurveysController _controller;

    public ValidationTests()
    {
        // Створюємо нову базу в пам'яті для кожного запуску тестів
        var options = new DbContextOptionsBuilder<SurveyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new SurveyDbContext(options);
        _controller = new SurveysController(_context);
    }

    [Fact]
    public async Task Respond_ShouldFail_WhenRatingIsOutOfBounds()
    {
        // Arrange: Створюємо опитування з питанням типу Rating
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = CreateValidSurvey(surveyId, questionId, QuestionType.Rating);
        
        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();

        var response = new Response
        {
            RespondentEmail = "user@test.com",
            Answers = new List<Answer> { new() { QuestionId = questionId, Value = "6" } } // Невалідно (більше 5)
        };

        // Act
        var result = await _controller.Respond(surveyId, response);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("від 1 до 5");
    }

    [Fact]
    public async Task Respond_ShouldFail_WhenRequiredQuestionIsMissing()
    {
        // Arrange: Питання позначене як IsRequired = true
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = CreateValidSurvey(surveyId, questionId);
        survey.Questions[0].IsRequired = true;
        
        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();

        var response = new Response
        {
            RespondentEmail = "user@test.com",
            Answers = new List<Answer>() // Відповіді порожні
        };

        // Act
        var result = await _controller.Respond(surveyId, response);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("є обов'язковим");
    }

    [Fact]
    public async Task Respond_ShouldFail_WhenSurveyIsExpired()
    {
        // Arrange: Опитування прострочене
        var surveyId = Guid.NewGuid();
        var survey = CreateValidSurvey(surveyId, Guid.NewGuid());
        survey.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        
        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();

        var response = new Response { RespondentEmail = "test@test.com" };

        // Act
        var result = await _controller.Respond(surveyId, response);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("неактивне або завершене");
    }

    // Допоміжний метод для створення валідного об'єкта Survey
    private Survey CreateValidSurvey(Guid surveyId, Guid questionId, QuestionType type = QuestionType.Text)
    {
        return new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            Questions = new List<Question>
            {
                new() { Id = questionId, SurveyId = surveyId, Text = "Test Question", Type = type }
            }
        };
    }
}