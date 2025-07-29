namespace BlazorQuizApp.Configuration
{
    public class AppSettings
    {
        public AdminUserSettings AdminUser { get; set; } = new();
        public QuizSettings Quiz { get; set; } = new();
    }

    public class AdminUserSettings
    {
        public string Email { get; set; } = string.Empty;
    }

    public class QuizSettings
    {
        public int DefaultTimeLimit { get; set; } = 300; // 5 minutes in seconds
        public int MaxQuestionsPerQuiz { get; set; } = 10;
        public bool RandomizeQuestions { get; set; } = true;
    }
} 