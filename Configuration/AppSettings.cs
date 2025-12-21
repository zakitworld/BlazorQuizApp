namespace BlazorQuizApp.Configuration
{
    public class AppSettings
    {
        public AdminUserSettings AdminUser { get; set; } = new();
        public QuizSettings Quiz { get; set; } = new();
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class AdminUserSettings
    {
        public string Email { get; set; } = string.Empty;
    }

    public class QuizSettings
    {
        public int DefaultTimeLimit { get; set; } = 300;
        public int MaxQuestionsPerQuiz { get; set; } = 10;
        public bool RandomizeQuestions { get; set; } = true;
        public int FreeMonthlyLimit { get; set; } = 5;
    }

    public class EmailSettings
    {
        public string SendGridApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class StripeSettings
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public PricingIds PricingIds { get; set; } = new();
    }

    public class PricingIds
    {
        public string PremiumMonthly { get; set; } = string.Empty;
        public string ProMonthly { get; set; } = string.Empty;
    }
}