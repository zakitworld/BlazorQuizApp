using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorQuizApp.Services
{
    /// <summary>
    /// Service interface for managing user subscriptions and usage tracking
    /// </summary>
    public interface ISubscriptionService
    {
        Task<Subscription?> GetUserSubscriptionAsync(string userId);
        Task<SubscriptionTier> GetUserTierAsync(string userId);
        Task<bool> CanTakeQuizAsync(string userId);
        Task IncrementQuizUsageAsync(string userId);
        Task<int> GetMonthlyQuizCountAsync(string userId);
        Task<bool> CreateSubscriptionAsync(string userId, SubscriptionTier tier, string? stripeSubscriptionId = null);
        Task<bool> UpdateSubscriptionAsync(string userId, SubscriptionTier tier);
        Task<bool> CancelSubscriptionAsync(string userId);
    }

    /// <summary>
    /// Service for managing user subscriptions, tracking quiz usage,
    /// and enforcing subscription tier limits.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<Subscription?> GetUserSubscriptionAsync(string userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await context.Subscriptions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription for user {UserId}", userId);
                return null;
            }
        }

        public async Task<SubscriptionTier> GetUserTierAsync(string userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            return subscription?.Tier ?? SubscriptionTier.Free;
        }

        public async Task<bool> CanTakeQuizAsync(string userId)
        {
            try
            {
                var tier = await GetUserTierAsync(userId);

                // Premium and above have unlimited quizzes
                if (tier != SubscriptionTier.Free)
                    return true;

                // Check free tier limit
                var monthlyCount = await GetMonthlyQuizCountAsync(userId);
                var limit = tier.GetMonthlyQuizLimit();

                return monthlyCount < limit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quiz eligibility for user {UserId}", userId);
                return false;
            }
        }

        public async Task IncrementQuizUsageAsync(string userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                var usage = await context.UsageTrackings
                    .FirstOrDefaultAsync(u => u.UserId == userId &&
                                            u.Month == now.Month &&
                                            u.Year == now.Year);

                if (usage == null)
                {
                    usage = new UsageTracking
                    {
                        UserId = userId,
                        Month = now.Month,
                        Year = now.Year,
                        QuizzesTaken = 1
                    };
                    context.UsageTrackings.Add(usage);
                }
                else
                {
                    usage.QuizzesTaken++;
                    usage.UpdatedAt = now;
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing quiz usage for user {UserId}", userId);
            }
        }

        public async Task<int> GetMonthlyQuizCountAsync(string userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                var usage = await context.UsageTrackings
                    .FirstOrDefaultAsync(u => u.UserId == userId &&
                                            u.Month == now.Month &&
                                            u.Year == now.Year);

                return usage?.QuizzesTaken ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly quiz count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> CreateSubscriptionAsync(string userId, SubscriptionTier tier, string? stripeSubscriptionId = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Deactivate existing subscriptions
                var existingSubscriptions = await context.Subscriptions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                foreach (var sub in existingSubscriptions)
                {
                    sub.IsActive = false;
                    sub.EndDate = DateTime.UtcNow;
                    sub.UpdatedAt = DateTime.UtcNow;
                }

                // Create new subscription
                var subscription = new Subscription
                {
                    UserId = userId,
                    Tier = tier,
                    StripeSubscriptionId = stripeSubscriptionId,
                    IsActive = true,
                    StartDate = DateTime.UtcNow
                };

                context.Subscriptions.Add(subscription);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created {Tier} subscription for user {UserId}", tier, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateSubscriptionAsync(string userId, SubscriptionTier tier)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var subscription = await context.Subscriptions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                    return await CreateSubscriptionAsync(userId, tier);

                subscription.Tier = tier;
                subscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription to {Tier} for user {UserId}", tier, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(string userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var subscription = await context.Subscriptions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                    return false;

                subscription.IsActive = false;
                subscription.EndDate = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation("Cancelled subscription for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
                return false;
            }
        }
    }
}
