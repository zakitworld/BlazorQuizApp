using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public enum SubscriptionTier
    {
        Free = 0,
        Premium = 1,
        Pro = 2,
        Enterprise = 3
    }

    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

        [StringLength(100)]
        public string? StripeSubscriptionId { get; set; }

        [StringLength(100)]
        public string? StripeCustomerId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AutoRenew { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class UsageTracking
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        public int Month { get; set; }
        public int Year { get; set; }
        public int QuizzesTaken { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public static class SubscriptionTierExtensions
    {
        public static string GetDisplayName(this SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Free => "Free",
                SubscriptionTier.Premium => "Premium",
                SubscriptionTier.Pro => "Pro",
                SubscriptionTier.Enterprise => "Enterprise",
                _ => "Unknown"
            };
        }

        public static decimal GetMonthlyPrice(this SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Free => 0m,
                SubscriptionTier.Premium => 25m,      // GHS 25.00
                SubscriptionTier.Pro => 75m,          // GHS 75.00
                SubscriptionTier.Enterprise => 250m, // GHS 250.00
                _ => 0m
            };
        }

        public static int GetMonthlyQuizLimit(this SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Free => 5,
                SubscriptionTier.Premium => int.MaxValue,
                SubscriptionTier.Pro => int.MaxValue,
                SubscriptionTier.Enterprise => int.MaxValue,
                _ => 0
            };
        }

        public static List<string> GetFeatures(this SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Free => new List<string>
                {
                    "5 quizzes per month",
                    "Basic leaderboard access",
                    "Standard question pool"
                },
                SubscriptionTier.Premium => new List<string>
                {
                    "Unlimited quizzes",
                    "Advanced analytics dashboard",
                    "Ad-free experience",
                    "Export quiz results",
                    "Priority support"
                },
                SubscriptionTier.Pro => new List<string>
                {
                    "Everything in Premium",
                    "Custom quiz creation",
                    "API access",
                    "Team collaboration (10 users)",
                    "Custom branding",
                    "Advanced reporting"
                },
                SubscriptionTier.Enterprise => new List<string>
                {
                    "Everything in Pro",
                    "Unlimited team members",
                    "SSO integration",
                    "Dedicated support",
                    "Custom features",
                    "White-label options"
                },
                _ => new List<string>()
            };
        }
    }
}
