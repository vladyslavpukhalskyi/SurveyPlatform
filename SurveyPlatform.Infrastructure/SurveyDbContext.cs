using Microsoft.EntityFrameworkCore;
using SurveyPlatform.Core.Entities;

namespace SurveyPlatform.Infrastructure;

public class SurveyDbContext : DbContext
{
    public SurveyDbContext(DbContextOptions<SurveyDbContext> options) : base(options) { }

    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Бізнес-правило: Одна відповідь на email на одне опитування
        modelBuilder.Entity<Response>()
            .HasIndex(r => new { r.SurveyId, r.RespondentEmail })
            .IsUnique();

        // 2. Зв'язок Survey -> Questions
        modelBuilder.Entity<Survey>()
            .HasMany(s => s.Questions)
            .WithOne() 
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Зв'язок Question -> Options
        modelBuilder.Entity<Question>()
            .HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 4. Зв'язок Survey -> Responses
        modelBuilder.Entity<Survey>()
            .HasMany(s => s.Responses)
            .WithOne(r => r.Survey) // ВИПРАВЛЕНО: Вказали навігаційну властивість
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        // 5. Зв'язок Response -> Answers
        modelBuilder.Entity<Response>()
            .HasMany(r => r.Answers)
            .WithOne(a => a.Response)
            .HasForeignKey(a => a.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        // 6. Зв'язок Answer -> Question
        modelBuilder.Entity<Answer>()
            .HasOne(a => a.Question)
            .WithMany(q => q.Answers) // ВИПРАВЛЕНО: Вказали навігаційну властивість
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 7. Додаткові налаштування порядку
        modelBuilder.Entity<Question>().Property(q => q.Order).IsRequired();
        modelBuilder.Entity<Option>().Property(o => o.Order).IsRequired();
    }
}