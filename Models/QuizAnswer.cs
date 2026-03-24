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
        
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
        public int? TimeTakenSeconds { get; set; }
        public int? QuizResultId { get; set; }
    }
} 