namespace BlazorQuizApp.Configuration
{
    public class AppSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:7233";
        public QuizSettings Quiz { get; set; } = new();
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
    }

    public class QuizSettings
    {
        // Seconds allocated per question, by difficulty
        public int EasySecondsPerQuestion { get; set; } = 30;
        public int MediumSecondsPerQuestion { get; set; } = 45;
        public int HardSecondsPerQuestion { get; set; } = 60;

        // Points awarded per correct answer, by difficulty
        public int EasyPoints { get; set; } = 1;
        public int MediumPoints { get; set; } = 2;
        public int HardPoints { get; set; } = 3;

        // Speed bonus points when answered in under 50% of allocated time
        public int SpeedBonusPoints { get; set; } = 1;

        public bool RandomizeQuestions { get; set; } = true;
        public int MaxQuestionsPerQuiz { get; set; } = 20;
        public int FreeMonthlyLimit { get; set; } = 5;

        public int GetSecondsForDifficulty(string difficulty) => difficulty switch
        {
            "Easy" => EasySecondsPerQuestion,
            "Hard" => HardSecondsPerQuestion,
            _ => MediumSecondsPerQuestion
        };

        public int GetPointsForDifficulty(string difficulty) => difficulty switch
        {
            "Easy" => EasyPoints,
            "Hard" => HardPoints,
            _ => MediumPoints
        };
    }
}
