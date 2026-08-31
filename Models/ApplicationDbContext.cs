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
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = default!;
        public DbSet<QuizTemplate> QuizTemplates { get; set; } = default!;
        public DbSet<QuizTemplateQuestion> QuizTemplateQuestions { get; set; } = default!;
        public DbSet<QuizAssignment> QuizAssignments { get; set; } = default!;
        public DbSet<UserGroup> UserGroups { get; set; } = default!;
        public DbSet<UserGroupMember> UserGroupMembers { get; set; } = default!;
        public DbSet<AssignmentEnrollment> AssignmentEnrollments { get; set; } = default!;
        public DbSet<QuestionVersion> QuestionVersions { get; set; } = default!;
        public DbSet<QuizCategory> QuizCategories { get; set; } = default!;

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

            builder.Entity<UsageTracking>()
                .HasIndex(u => new { u.UserId, u.Year, u.Month })
                .IsUnique();

            builder.Entity<PaymentTransaction>()
                .HasIndex(p => p.Reference)
                .IsUnique();

            builder.Entity<QuizTemplateQuestion>().HasKey(x => new { x.QuizTemplateId, x.QuizQuestionId });
            builder.Entity<QuizTemplateQuestion>().HasOne(x => x.QuizTemplate).WithMany(x => x.TemplateQuestions)
                .HasForeignKey(x => x.QuizTemplateId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<QuizTemplateQuestion>().HasOne(x => x.QuizQuestion).WithMany()
                .HasForeignKey(x => x.QuizQuestionId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserGroupMember>().HasKey(x => new { x.UserGroupId, x.UserId });
            builder.Entity<UserGroupMember>().HasOne(x => x.UserGroup).WithMany(x => x.Members)
                .HasForeignKey(x => x.UserGroupId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizAssignment>().HasOne(x => x.QuizTemplate).WithMany(x => x.Assignments)
                .HasForeignKey(x => x.QuizTemplateId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<QuizAssignment>().HasOne(x => x.UserGroup).WithMany()
                .HasForeignKey(x => x.UserGroupId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<QuizAssignment>().HasIndex(x => x.AccessCodeHash).IsUnique();

            builder.Entity<QuizResult>().HasOne(x => x.QuizAssignment).WithMany()
                .HasForeignKey(x => x.QuizAssignmentId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<QuizResult>().HasIndex(x => new { x.QuizAssignmentId, x.UserId, x.AttemptNumber });

            builder.Entity<AssignmentEnrollment>().HasKey(x => new { x.QuizAssignmentId, x.UserId });
            builder.Entity<AssignmentEnrollment>().HasOne(x => x.QuizAssignment).WithMany()
                .HasForeignKey(x => x.QuizAssignmentId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuestionVersion>().HasIndex(x => new { x.QuizQuestionId, x.VersionNumber }).IsUnique();
            builder.Entity<QuestionVersion>().HasOne(x => x.QuizQuestion).WithMany()
                .HasForeignKey(x => x.QuizQuestionId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizCategory>().HasIndex(x => x.Name).IsUnique();
        }
    }
}
