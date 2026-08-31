using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BlazorQuizApp.Services;

public record AssignmentLaunch(QuizAssignment Assignment, List<QuizQuestion> Questions, int AttemptNumber);

public interface IAssignmentService
{
    Task<List<QuizTemplate>> GetTemplatesAsync(string ownerId);
    Task<QuizTemplate> CreateTemplateAsync(string ownerId, string title, string? description,
        int timeLimitMinutes, int passPercentage, bool adaptive, IEnumerable<int> questionIds);
    Task<(QuizAssignment Assignment, string AccessCode)> CreateAssignmentAsync(string ownerId, int templateId,
        string title, int? groupId, DateTime availableFrom, DateTime? availableUntil, int maxAttempts);
    Task<List<QuizAssignment>> GetAssignmentsForOwnerAsync(string ownerId);
    Task<List<QuizAssignment>> GetAvailableAssignmentsAsync(string userId);
    Task<int?> RedeemAccessCodeAsync(string userId, string accessCode);
    Task<AssignmentLaunch?> LaunchAsync(int assignmentId, string userId);
    Task<QuizResult?> SubmitAsync(int assignmentId, string userId, List<QuizAnswer> answers, TimeSpan duration);
    Task<int?> GetNextAdaptiveQuestionIdAsync(int assignmentId, string userId, List<QuizAnswer> answers);
    Task<List<UserGroup>> GetGroupsAsync(string ownerId);
    Task<UserGroup> CreateGroupAsync(string ownerId, string name, string? description);
    Task<bool> AddGroupMemberAsync(int groupId, string ownerId, string email);
}

public sealed class AssignmentService(
    ApplicationDbContext db,
    IQuizBusinessService quizService,
    IAuditService auditService) : IAssignmentService
{
    public Task<List<QuizTemplate>> GetTemplatesAsync(string ownerId) => db.QuizTemplates.AsNoTracking()
        .Where(x => x.OwnerId == ownerId).Include(x => x.TemplateQuestions)
        .OrderByDescending(x => x.CreatedAt).ToListAsync();

    public async Task<QuizTemplate> CreateTemplateAsync(string ownerId, string title, string? description,
        int timeLimitMinutes, int passPercentage, bool adaptive, IEnumerable<int> questionIds)
    {
        var ids = questionIds.Distinct().ToList();
        if (ids.Count == 0) throw new ArgumentException("Select at least one question.");
        if (await db.QuizQuestions.CountAsync(q => ids.Contains(q.Id)) != ids.Count)
            throw new ArgumentException("One or more questions do not exist.");

        var template = new QuizTemplate
        {
            OwnerId = ownerId, Title = title.Trim(), Description = description?.Trim(),
            TimeLimitMinutes = Math.Clamp(timeLimitMinutes, 1, 240),
            PassPercentage = Math.Clamp(passPercentage, 1, 100), IsAdaptive = adaptive,
            TemplateQuestions = ids.Select((id, index) => new QuizTemplateQuestion
                { QuizQuestionId = id, SortOrder = index }).ToList()
        };
        db.QuizTemplates.Add(template);
        await db.SaveChangesAsync();
        await auditService.WriteAsync("QuizTemplateCreated", nameof(QuizTemplate), template.Id, template.Title, ownerId);
        return template;
    }

    public async Task<(QuizAssignment Assignment, string AccessCode)> CreateAssignmentAsync(string ownerId,
        int templateId, string title, int? groupId, DateTime availableFrom, DateTime? availableUntil, int maxAttempts)
    {
        if (!await db.QuizTemplates.AnyAsync(x => x.Id == templateId && x.OwnerId == ownerId))
            throw new InvalidOperationException("Template not found.");
        if (groupId.HasValue && !await db.UserGroups.AnyAsync(x => x.Id == groupId && x.OwnerId == ownerId))
            throw new InvalidOperationException("Group not found.");
        if (availableUntil.HasValue && availableUntil <= availableFrom)
            throw new ArgumentException("The closing date must be after the opening date.");

        var code = GenerateCode();
        var assignment = new QuizAssignment
        {
            OwnerId = ownerId, QuizTemplateId = templateId, UserGroupId = groupId,
            Title = title.Trim(), AvailableFrom = availableFrom.ToUniversalTime(),
            AvailableUntil = availableUntil?.ToUniversalTime(), MaxAttempts = Math.Clamp(maxAttempts, 1, 20),
            AccessCodeHash = HashCode(code), AccessCodeHint = code[^4..]
        };
        db.QuizAssignments.Add(assignment);
        await db.SaveChangesAsync();
        await auditService.WriteAsync("AssignmentCreated", nameof(QuizAssignment), assignment.Id, assignment.Title, ownerId);
        return (assignment, code);
    }

    public Task<List<QuizAssignment>> GetAssignmentsForOwnerAsync(string ownerId) => db.QuizAssignments.AsNoTracking()
        .Where(x => x.OwnerId == ownerId).Include(x => x.QuizTemplate).Include(x => x.UserGroup)
        .OrderByDescending(x => x.CreatedAt).ToListAsync();

    public Task<List<QuizAssignment>> GetAvailableAssignmentsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return db.QuizAssignments.AsNoTracking().Where(a => a.IsPublished && a.AvailableFrom <= now &&
                (!a.AvailableUntil.HasValue || a.AvailableUntil > now) &&
                (db.AssignmentEnrollments.Any(e => e.QuizAssignmentId == a.Id && e.UserId == userId) ||
                 (a.UserGroupId.HasValue && db.UserGroupMembers.Any(m => m.UserGroupId == a.UserGroupId && m.UserId == userId))))
            .Include(a => a.QuizTemplate).OrderBy(a => a.AvailableUntil).ToListAsync();
    }

    public async Task<int?> RedeemAccessCodeAsync(string userId, string accessCode)
    {
        var hash = HashCode(accessCode.Trim().ToUpperInvariant());
        var assignment = await db.QuizAssignments.SingleOrDefaultAsync(a => a.AccessCodeHash == hash && a.IsPublished);
        if (assignment == null) return null;
        if (assignment.UserGroupId.HasValue && !await db.UserGroupMembers.AnyAsync(m =>
                m.UserGroupId == assignment.UserGroupId && m.UserId == userId)) return null;
        if (!await db.AssignmentEnrollments.AnyAsync(e => e.QuizAssignmentId == assignment.Id && e.UserId == userId))
        {
            db.AssignmentEnrollments.Add(new AssignmentEnrollment { QuizAssignmentId = assignment.Id, UserId = userId });
            await db.SaveChangesAsync();
        }
        return assignment.Id;
    }

    public async Task<AssignmentLaunch?> LaunchAsync(int assignmentId, string userId)
    {
        var now = DateTime.UtcNow;
        var assignment = await db.QuizAssignments.AsNoTracking().Include(a => a.QuizTemplate)
            .ThenInclude(t => t.TemplateQuestions).ThenInclude(tq => tq.QuizQuestion).ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(a => a.Id == assignmentId && a.IsPublished);
        if (assignment == null || now < assignment.AvailableFrom ||
            (assignment.AvailableUntil.HasValue && now >= assignment.AvailableUntil)) return null;
        var allowed = await db.AssignmentEnrollments.AnyAsync(e => e.QuizAssignmentId == assignmentId && e.UserId == userId) ||
            (assignment.UserGroupId.HasValue && await db.UserGroupMembers.AnyAsync(m => m.UserGroupId == assignment.UserGroupId && m.UserId == userId));
        if (!allowed) return null;
        var attempts = await db.QuizResults.CountAsync(r => r.QuizAssignmentId == assignmentId && r.UserId == userId);
        if (attempts >= assignment.MaxAttempts) return null;

        var questions = assignment.QuizTemplate.TemplateQuestions.OrderBy(x => x.SortOrder).Select(x => x.QuizQuestion).ToList();
        if (assignment.QuizTemplate.IsAdaptive)
        {
            var prior = await db.QuizResults.Where(r => r.UserId == userId).OrderByDescending(r => r.CompletedAt)
                .Take(5).Select(r => r.TotalQuestions == 0 ? 0 : (double)r.CorrectAnswers / r.TotalQuestions * 100).ToListAsync();
            var average = prior.Count == 0 ? 50 : prior.Average();
            questions = average >= 75
                ? questions.OrderByDescending(q => DifficultyRank(q.Difficulty)).ToList()
                : questions.OrderBy(q => DifficultyRank(q.Difficulty)).ToList();
        }
        else if (assignment.QuizTemplate.RandomizeQuestions)
            questions = questions.OrderBy(_ => Random.Shared.Next()).ToList();
        return new AssignmentLaunch(assignment, questions, attempts + 1);
    }

    public async Task<QuizResult?> SubmitAsync(int assignmentId, string userId, List<QuizAnswer> answers, TimeSpan duration)
    {
        var launch = await LaunchAsync(assignmentId, userId);
        if (launch == null || duration > TimeSpan.FromMinutes(launch.Assignment.QuizTemplate.TimeLimitMinutes + 2)) return null;
        var allowedIds = launch.Questions.Select(q => q.Id).ToHashSet();
        if (answers.Any(a => !allowedIds.Contains(a.QuestionId))) return null;
        var result = await quizService.SubmitQuizAsync(answers, userId, duration, launch.Assignment.Title,
            false, 0, assignmentId, launch.AttemptNumber, launch.Assignment.QuizTemplate.PassPercentage);
        await auditService.WriteAsync("AssignmentSubmitted", nameof(QuizAssignment), assignmentId,
            $"Attempt {launch.AttemptNumber}; {result.ScorePercentage:F0}%", userId);
        return result;
    }

    public async Task<int?> GetNextAdaptiveQuestionIdAsync(int assignmentId, string userId, List<QuizAnswer> answers)
    {
        var assignment = await db.QuizAssignments.AsNoTracking().Include(a => a.QuizTemplate)
            .ThenInclude(t => t.TemplateQuestions).ThenInclude(tq => tq.QuizQuestion).ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(a => a.Id == assignmentId && a.QuizTemplate.IsAdaptive && a.IsPublished);
        if (assignment == null) return null;
        var enrolled = await db.AssignmentEnrollments.AnyAsync(e => e.QuizAssignmentId == assignmentId && e.UserId == userId) ||
            (assignment.UserGroupId.HasValue && await db.UserGroupMembers.AnyAsync(m => m.UserGroupId == assignment.UserGroupId && m.UserId == userId));
        if (!enrolled) return null;
        var all = assignment.QuizTemplate.TemplateQuestions.Select(x => x.QuizQuestion).ToList();
        var answeredIds = answers.Select(a => a.QuestionId).ToHashSet();
        var remaining = all.Where(q => !answeredIds.Contains(q.Id)).ToList();
        if (remaining.Count == 0) return null;
        var correct = answers.Count(a => all.FirstOrDefault(q => q.Id == a.QuestionId) is { } q && QuizBusinessService.IsAnswerCorrect(q, a));
        var rate = answers.Count == 0 ? .5 : (double)correct / answers.Count;
        var target = rate >= .75 ? 3 : rate <= .4 ? 1 : 2;
        return remaining.OrderBy(q => Math.Abs(DifficultyRank(q.Difficulty) - target))
            .ThenBy(_ => Random.Shared.Next()).First().Id;
    }

    public Task<List<UserGroup>> GetGroupsAsync(string ownerId) => db.UserGroups.AsNoTracking()
        .Where(x => x.OwnerId == ownerId).Include(x => x.Members).OrderBy(x => x.Name).ToListAsync();

    public async Task<UserGroup> CreateGroupAsync(string ownerId, string name, string? description)
    {
        var group = new UserGroup { OwnerId = ownerId, Name = name.Trim(), Description = description?.Trim() };
        db.UserGroups.Add(group); await db.SaveChangesAsync(); return group;
    }

    public async Task<bool> AddGroupMemberAsync(int groupId, string ownerId, string email)
    {
        var group = await db.UserGroups.SingleOrDefaultAsync(x => x.Id == groupId && x.OwnerId == ownerId);
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == email.Trim().ToUpper());
        if (group == null || user == null) return false;
        if (!await db.UserGroupMembers.AnyAsync(x => x.UserGroupId == groupId && x.UserId == user.Id))
        { db.UserGroupMembers.Add(new UserGroupMember { UserGroupId = groupId, UserId = user.Id }); await db.SaveChangesAsync(); }
        return true;
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(8, chars, (span, alphabet) =>
        { for (var i = 0; i < span.Length; i++) span[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]; });
    }
    private static string HashCode(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    private static int DifficultyRank(string difficulty) => difficulty switch { "Easy" => 1, "Hard" => 3, _ => 2 };
}
