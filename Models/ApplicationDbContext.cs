using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorQuizApp.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<QuizResult> QuizResults { get; set; } = default!;
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = default!;
        public DbSet<QuizOption> QuizOptions { get; set; } = default!;
        public DbSet<QuizAnswer> QuizAnswers { get; set; } = default!;
        public DbSet<Subscription> Subscriptions { get; set; } = default!;
        public DbSet<UsageTracking> UsageTrackings { get; set; } = default!;
        public DbSet<AuditLog> AuditLogs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure QuizQuestion -> QuizOptions relationship
            builder.Entity<QuizQuestion>()
                .HasMany(q => q.Options)
                .WithOne()
                .HasForeignKey(o => o.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure QuizResult relationship with User if needed
            builder.Entity<QuizResult>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .IsRequired();
                
            // Configure QuizResult -> QuizAnswer relationship
            builder.Entity<QuizResult>()
                .HasMany(r => r.Answers)
                .WithOne()
                .HasForeignKey(a => a.QuizResultId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Subscription relationship with User if needed
            builder.Entity<Subscription>()
                .HasIndex(s => s.UserId)
                .IsUnique();
        }
    }
}
