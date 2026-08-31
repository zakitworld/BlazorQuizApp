using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models;

public class QuizTemplate
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required, StringLength(450)] public string OwnerId { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; } = 30;
    public int PassPercentage { get; set; } = 70;
    public bool IsAdaptive { get; set; }
    public bool RandomizeQuestions { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<QuizTemplateQuestion> TemplateQuestions { get; set; } = [];
    public List<QuizAssignment> Assignments { get; set; } = [];
}

public class QuizTemplateQuestion
{
    public int QuizTemplateId { get; set; }
    public QuizTemplate QuizTemplate { get; set; } = default!;
    public int QuizQuestionId { get; set; }
    public QuizQuestion QuizQuestion { get; set; } = default!;
    public int SortOrder { get; set; }
}

public class UserGroup
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Required, StringLength(450)] public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<UserGroupMember> Members { get; set; } = [];
}

public class UserGroupMember
{
    public int UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = default!;
    [Required, StringLength(450)] public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class QuizAssignment
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string Title { get; set; } = string.Empty;
    public int QuizTemplateId { get; set; }
    public QuizTemplate QuizTemplate { get; set; } = default!;
    public int? UserGroupId { get; set; }
    public UserGroup? UserGroup { get; set; }
    [Required, StringLength(450)] public string OwnerId { get; set; } = string.Empty;
    [Required, StringLength(64)] public string AccessCodeHash { get; set; } = string.Empty;
    [StringLength(12)] public string AccessCodeHint { get; set; } = string.Empty;
    public DateTime AvailableFrom { get; set; } = DateTime.UtcNow;
    public DateTime? AvailableUntil { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AssignmentEnrollment
{
    public int QuizAssignmentId { get; set; }
    public QuizAssignment QuizAssignment { get; set; } = default!;
    [Required, StringLength(450)] public string UserId { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
