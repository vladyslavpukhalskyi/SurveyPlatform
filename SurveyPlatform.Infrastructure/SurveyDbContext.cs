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

        // 2. Налаштування зв'язків та каскадного видалення
        // Видаляємо опитування — видаляються всі питання
        modelBuilder.Entity<Survey>()
            .HasMany(s => s.Questions)
            .WithOne(q => q.Survey)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Видаляємо питання — видаляються варіанти відповідей (Options)
        modelBuilder.Entity<Question>()
            .HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // 3. Зберігаємо порядок питань та опцій
        modelBuilder.Entity<Question>().Property(q => q.Order).IsRequired();
        modelBuilder.Entity<Option>().Property(o => o.Order).IsRequired();
    }
}