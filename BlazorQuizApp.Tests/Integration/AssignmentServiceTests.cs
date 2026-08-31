using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using BlazorQuizApp.Tests.Helpers;
using Moq;
using Xunit;

namespace BlazorQuizApp.Tests.Integration;

public class AssignmentServiceTests
{
    private static AssignmentService Create(ApplicationDbContext db) => new(db,
        Mock.Of<IQuizBusinessService>(), Mock.Of<IAuditService>());

    private static (ApplicationUser owner, ApplicationUser learner, QuizQuestion question) Seed(ApplicationDbContext db)
    {
        var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com", NormalizedEmail = "OWNER@TEST.COM" };
        var learner = new ApplicationUser { Id = "learner", Email = "learner@test.com", NormalizedEmail = "LEARNER@TEST.COM" };
        var question = new QuizQuestion { QuestionText = "A sufficiently long test question?", Category = "General", Difficulty = "Easy",
            Options = [new QuizOption { Text = "Yes" }, new QuizOption { Text = "No" }] };
        db.AddRange(owner, learner, question); db.SaveChanges(); question.CorrectOptionId = question.Options[0].Id; db.SaveChanges();
        return (owner, learner, question);
    }

    [Fact]
    public async Task AccessCode_EnrollsLearner_WithoutStoringPlaintext()
    {
        using var db = DbContextHelper.Create(); var data = Seed(db); var service = Create(db);
        var template = await service.CreateTemplateAsync(data.owner.Id, "Test", null, 10, 70, false, [data.question.Id]);
        var created = await service.CreateAssignmentAsync(data.owner.Id, template.Id, "Checkpoint", null, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(1), 1);

        Assert.DoesNotContain(created.AccessCode, created.Assignment.AccessCodeHash);
        Assert.Equal(created.Assignment.Id, await service.RedeemAccessCodeAsync(data.learner.Id, created.AccessCode));
        Assert.Single(db.AssignmentEnrollments);
    }

    [Fact]
    public async Task Launch_RejectsLearnerWithoutEnrollment()
    {
        using var db = DbContextHelper.Create(); var data = Seed(db); var service = Create(db);
        var template = await service.CreateTemplateAsync(data.owner.Id, "Test", null, 10, 70, false, [data.question.Id]);
        var created = await service.CreateAssignmentAsync(data.owner.Id, template.Id, "Checkpoint", null, DateTime.UtcNow.AddMinutes(-1), null, 1);
        Assert.Null(await service.LaunchAsync(created.Assignment.Id, data.learner.Id));
    }

    [Fact]
    public async Task Launch_RejectsExpiredAssignment()
    {
        using var db = DbContextHelper.Create(); var data = Seed(db); var service = Create(db);
        var template = await service.CreateTemplateAsync(data.owner.Id, "Test", null, 10, 70, false, [data.question.Id]);
        var created = await service.CreateAssignmentAsync(data.owner.Id, template.Id, "Old", null, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), 1);
        await service.RedeemAccessCodeAsync(data.learner.Id, created.AccessCode);
        Assert.Null(await service.LaunchAsync(created.Assignment.Id, data.learner.Id));
    }
}
