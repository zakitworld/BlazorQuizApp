namespace BlazorQuizApp.Models
{
    public class NewQuestionForm
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<string> OptionTexts { get; set; } = new List<string> { "", "", "", "" };
        public int CorrectOptionIndex { get; set; } = 0;
    }
}
