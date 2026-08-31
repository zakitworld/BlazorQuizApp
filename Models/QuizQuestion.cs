using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public enum QuestionType
    {
        MultipleChoice,
        MultipleSelect,
        TrueFalse,
        ShortAnswer
    }

    public class QuizQuestion
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string QuestionText { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Category { get; set; } = "General";
        
        [StringLength(50)]
        public string Difficulty { get; set; } = "Medium";
        
        public List<QuizOption> Options { get; set; } = new List<QuizOption>();
        public int SelectedOptionId { get; set; } = -1;
        public int CorrectOptionId { get; set; }
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
        [StringLength(2000)] public string? CorrectOptionIdsJson { get; set; }
        [StringLength(2000)] public string? AcceptedAnswersJson { get; set; }
        public int CurrentVersion { get; set; } = 1;
        
        [StringLength(2000)]
        public string? Explanation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class QuestionVersion
    {
        public long Id { get; set; }
        public int QuizQuestionId { get; set; }
        public QuizQuestion QuizQuestion { get; set; } = default!;
        public int VersionNumber { get; set; }
        [Required, StringLength(1000)] public string QuestionText { get; set; } = string.Empty;
        [Required] public string OptionsJson { get; set; } = "[]";
        public QuestionType Type { get; set; }
        [StringLength(2000)] public string? CorrectAnswersJson { get; set; }
        [StringLength(2000)] public string? Explanation { get; set; }
        [StringLength(450)] public string? ChangedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
