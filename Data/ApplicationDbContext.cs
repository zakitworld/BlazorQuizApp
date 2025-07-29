using BlazorQuizApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorQuizApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<QuizQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Difficulty).HasMaxLength(50);
                entity.HasMany(e => e.Options)
                      .WithOne()
                      .HasForeignKey("QuestionId")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<QuizOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired().HasMaxLength(500);
            });

            builder.Entity<QuizResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.HasMany(e => e.Answers)
                      .WithOne()
                      .HasForeignKey("QuizResultId")
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CompletedAt);
            });

            builder.Entity<QuizAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.QuestionId, e.SelectedOptionId });
            });
        }
    }
}
