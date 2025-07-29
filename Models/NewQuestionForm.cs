using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public class NewQuestionForm
    {
        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string QuestionText { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = "General";
        
        [Required]
        [StringLength(50)]
        public string Difficulty { get; set; } = "Medium";
        
        public List<string> OptionTexts { get; set; } = new List<string> { "", "", "", "" };
        public int CorrectOptionIndex { get; set; } = 0;
    }
}
