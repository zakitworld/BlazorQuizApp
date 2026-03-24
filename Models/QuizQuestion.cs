using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
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
        
        [StringLength(2000)]
        public string? Explanation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
