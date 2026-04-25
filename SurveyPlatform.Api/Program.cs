using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Додаємо підтримку контролерів (замість Minimal API)
builder.Services.AddControllers();

// 2. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Реєструємо DbContext з використанням PostgreSQL
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

// 4. Мапимо контролери
app.MapControllers();

// Автоматичне створення бази та наповнення при старті (тільки для розробки)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SurveyDbContext>();
    
    // Створюємо таблиці, якщо їх немає
    await context.Database.EnsureCreatedAsync();

    // Перевіряємо, чи база порожня
    if (!await context.Surveys.AnyAsync())
    {
        // Тут можна викликати логіку наповнення, яку ми писали для тестів
        // Або для швидкої перевірки просто додати одне опитування:
        context.Surveys.Add(new Survey 
        { 
            Id = Guid.NewGuid(), 
            Title = "Load Test Survey", 
            IsActive = true, 
            ExpiresAt = DateTime.UtcNow.AddDays(10) 
        });
        await context.SaveChangesAsync();
    }
}

app.Run();

public partial class Program { }