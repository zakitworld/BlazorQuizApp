namespace BlazorQuizApp.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<QuizOption> Options { get; set; } = new List<QuizOption>();
        public int SelectedOptionId { get; set; } = -1;
        public int CorrectOptionId { get; set; }
    }
}
