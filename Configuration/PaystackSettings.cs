namespace BlazorQuizApp.Configuration
{
    public class PaystackSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public PaystackPlans PricingPlans { get; set; } = new();
    }

    public class PaystackPlans
    {
        public string PremiumMonthly { get; set; } = string.Empty;
        public string ProMonthly { get; set; } = string.Empty;
    }
}
