using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public class QuizResult
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;
        
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double ScorePercentage => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions * 100 : 0;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public List<QuizAnswer> Answers { get; set; } = new();
    }
}
