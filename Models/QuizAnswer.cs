using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public class QuizAnswer
    {
        public int Id { get; set; }
        
        [Required]
        public int QuestionId { get; set; }
        
        [Required]
        public int SelectedOptionId { get; set; }
        [StringLength(2000)] public string? SelectedOptionIdsJson { get; set; }
        [StringLength(1000)] public string? TextAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int QuestionVersionNumber { get; set; } = 1;
        [StringLength(1000)] public string? QuestionTextSnapshot { get; set; }
        [StringLength(500)] public string? SelectedAnswerSnapshot { get; set; }
        [StringLength(500)] public string? CorrectAnswerSnapshot { get; set; }
        
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
        public int? TimeTakenSeconds { get; set; }
        public int? QuizResultId { get; set; }
    }
}
