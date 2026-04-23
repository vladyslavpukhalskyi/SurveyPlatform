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

app.Run();