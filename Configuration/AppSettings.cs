namespace BlazorQuizApp.Configuration
{
    public class AppSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:7233";
        public QuizSettings Quiz { get; set; } = new();
        public string AdminEmail { get; set; } = "admin@blazorquiz.com";
        public string AdminPassword { get; set; } = "Admin@123!";
    }

    public class QuizSettings
    {
        public int DefaultTimeLimit { get; set; } = 300; // 5 minutes
        public bool RandomizeQuestions { get; set; } = true;
    }
}
