using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurveyPlatform.Infrastructure;
using SurveyPlatform.Core.Entities;
using Testcontainers.PostgreSql;
using AutoFixture;
using Xunit;

namespace SurveyPlatform.Tests.Abstractions;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("survey_db")
        .WithUsername("admin")
        .WithPassword("password")
        .Build();

    protected HttpClient Client { get; private set; } = null!;
    protected IServiceScope Scope { get; private set; } = null!;
    protected SurveyDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => 
                        d.ServiceType == typeof(DbContextOptions<SurveyDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<SurveyDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString()));
                });
            });

        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<SurveyDbContext>();

        await DbContext.Database.EnsureCreatedAsync();
        await SeedDatabaseAsync();
    }

    private async Task SeedDatabaseAsync()
{
    var fixture = new Fixture();
    fixture.Customizations.Add(new ElementsBuilder<DateTime>(new[] { DateTime.UtcNow }));
    fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
        .ForEach(b => fixture.Behaviors.Remove(b));
    fixture.Behaviors.Add(new OmitOnRecursionBehavior());

    // 1. Створюємо 100 опитувань
    var surveys = fixture.Build<Survey>()
        .With(s => s.IsActive, true)
        .With(s => s.CreatedAt, DateTime.UtcNow)
        .With(s => s.ExpiresAt, DateTime.UtcNow.AddDays(10))
        .Without(s => s.Questions)
        .Without(s => s.Responses)
        .CreateMany(100).ToList();

    await DbContext.Surveys.AddRangeAsync(surveys);
    await DbContext.SaveChangesAsync(); // Опитування вже в базі

    var allQuestions = new List<Question>();
    var allOptions = new List<Option>();
    var allResponses = new List<Response>();

    // 2. Створюємо питання та респондентів вручну, щоб не було помилок з ID
    foreach (var survey in surveys)
    {
        for (int q = 0; q < 10; q++)
        {
            var question = new Question
            {
                Id = Guid.NewGuid(),
                SurveyId = survey.Id, // Пряме призначення гарантує успіх
                Text = $"Question {q} for survey {survey.Id}",
                Type = QuestionType.SingleChoice,
                IsRequired = true,
                Order = q
            };
            allQuestions.Add(question);

            for (int o = 0; o < 3; o++)
            {
                allOptions.Add(new Option
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Text = $"Option {o}",
                    Order = o
                });
            }
        }

        for (int r = 0; r < 10; r++)
        {
            allResponses.Add(new Response
            {
                Id = Guid.NewGuid(),
                SurveyId = survey.Id, // Пряме призначення
                RespondentEmail = $"user_{Guid.NewGuid().ToString()[..8]}@example.com",
                SubmittedAt = DateTime.UtcNow
            });
        }
    }

    // Зберігаємо структуру
    await DbContext.Questions.AddRangeAsync(allQuestions);
    await DbContext.SaveChangesAsync();

    await DbContext.Options.AddRangeAsync(allOptions);
    await DbContext.Responses.AddRangeAsync(allResponses);
    await DbContext.SaveChangesAsync();

    // 3. Створюємо відповіді (Answers)
    var allAnswers = new List<Answer>();
    foreach (var resp in allResponses)
    {
        var surveyQuestions = allQuestions.Where(q => q.SurveyId == resp.SurveyId);
        foreach (var q in surveyQuestions)
        {
            allAnswers.Add(new Answer
            {
                Id = Guid.NewGuid(),
                ResponseId = resp.Id,
                QuestionId = q.Id,
                Value = "Seed Answer"
            });
        }
    }

    await DbContext.Answers.AddRangeAsync(allAnswers);
    await DbContext.SaveChangesAsync();
}

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        Scope.Dispose();
    }
}