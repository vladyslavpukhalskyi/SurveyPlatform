using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurveyPlatform.Infrastructure;
using SurveyPlatform.Tests.DatabaseSeeder; // Додано Using
using Testcontainers.PostgreSql;
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
        
        DatabaseSeeder.DatabaseSeeder.Seed10KRecords(DbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        Scope.Dispose();
    }
}