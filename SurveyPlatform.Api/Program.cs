using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization; 
using SurveyPlatform.Infrastructure;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Core.Interfaces;
using SurveyPlatform.Infrastructure.Repositories; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SurveyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>(); 
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SurveyDbContext>();
    
    await context.Database.EnsureCreatedAsync();
    
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

public partial class Program { }