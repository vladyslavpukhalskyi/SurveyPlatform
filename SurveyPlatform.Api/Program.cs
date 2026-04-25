using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Infrastructure;
using SurveyPlatform.Core.Entities; // Додано для доступу до Survey

var builder = WebApplication.CreateBuilder(args);

// 1. Додаємо підтримку контролерів
builder.Services.AddControllers();

// 2. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Реєструємо DbContext
builder.Services.AddDbContext<SurveyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Налаштування HTTP-пайплайну
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 4. Автоматичне створення бази та наповнення
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SurveyDbContext>();
    
    // Створюємо таблиці, якщо їх немає
    await context.Database.EnsureCreatedAsync();

    // Наповнюємо ТІЛЬКИ в режимі розробки і якщо база порожня
    // Це виправить помилку "found 101" у твоїх тестах на GitHub
    if (app.Environment.IsDevelopment() && !await context.Surveys.AnyAsync())
    {
        context.Surveys.Add(new Survey 
        { 
            Id = Guid.NewGuid(), 
            Title = "Load Test Survey", 
            Description = "Initial survey for local testing",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow,
            IsActive = true, 
            ExpiresAt = DateTime.UtcNow.AddDays(10) 
        });
        await context.SaveChangesAsync();
    }
}

app.Run();

// Необхідно для роботи інтеграційних тестів
public partial class Program { }