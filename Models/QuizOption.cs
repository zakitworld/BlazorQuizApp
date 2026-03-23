using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public class QuizOption
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        public int QuizQuestionId { get; set; }
    }
}