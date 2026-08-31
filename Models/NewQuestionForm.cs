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
        
        [StringLength(2000)]
        public string? Explanation { get; set; }

        public List<string> OptionTexts { get; set; } = ["", "", "", ""];
        public int CorrectOptionIndex { get; set; } = 0;
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
        public List<int> CorrectOptionIndexes { get; set; } = [];
        [StringLength(1000)] public string? AcceptedAnswers { get; set; }
    }
}
