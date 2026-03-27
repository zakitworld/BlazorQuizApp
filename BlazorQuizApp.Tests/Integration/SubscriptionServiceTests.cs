using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using BlazorQuizApp.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlazorQuizApp.Tests.Integration
{
    /// <summary>
    /// Integration tests for SubscriptionService using an isolated in-memory database.
    ///
    /// NOTE: The ApplicationDbContext has a unique index on Subscription.UserId. The in-memory
    /// EF provider does NOT enforce unique indexes, so CreateSubscriptionAsync (which deactivates
    /// an old record and inserts a new one) will pass in tests but would violate the constraint on
    /// a real SQL Server. This is documented as a known limitation.
    /// </summary>
    public class SubscriptionServiceTests
    {
        private const string UserId = "user-abc-123";

        private SubscriptionService CreateService(IServiceProvider provider)
        {
            return new SubscriptionService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<SubscriptionService>.Instance);
        }

        // ── GetUserSubscriptionAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetUserSubscriptionAsync_NoSubscription_ReturnsNull()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var result = await svc.GetUserSubscriptionAsync(UserId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserSubscriptionAsync_ActiveSubscription_ReturnsIt()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId,
                    Tier = SubscriptionTier.Premium,
                    IsActive = true
                });
            });
            var svc = CreateService(provider);

            var result = await svc.GetUserSubscriptionAsync(UserId);

            Assert.NotNull(result);
            Assert.Equal(SubscriptionTier.Premium, result!.Tier);
        }

        [Fact]
        public async Task GetUserSubscriptionAsync_InactiveSubscription_ReturnsNull()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId,
                    Tier = SubscriptionTier.Premium,
                    IsActive = false
                });
            });
            var svc = CreateService(provider);

            var result = await svc.GetUserSubscriptionAsync(UserId);

            Assert.Null(result);
        }

        // ── GetUserTierAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetUserTierAsync_NoSubscription_ReturnsFree()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var tier = await svc.GetUserTierAsync(UserId);

            Assert.Equal(SubscriptionTier.Free, tier);
        }

        [Theory]
        [InlineData(SubscriptionTier.Premium)]
        [InlineData(SubscriptionTier.Pro)]
        [InlineData(SubscriptionTier.Enterprise)]
        public async Task GetUserTierAsync_WithActiveSubscription_ReturnsCorrectTier(
            SubscriptionTier expected)
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId,
                    Tier = expected,
                    IsActive = true
                });
            });
            var svc = CreateService(provider);

            var tier = await svc.GetUserTierAsync(UserId);

            Assert.Equal(expected, tier);
        }

        // ── CanTakeQuizAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task CanTakeQuizAsync_FreeTierUnderLimit_ReturnsTrue()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.UsageTrackings.Add(new UsageTracking
                {
                    UserId = UserId,
                    Month = DateTime.UtcNow.Month,
                    Year = DateTime.UtcNow.Year,
                    QuizzesTaken = 4 // under the free limit of 5
                });
            });
            var svc = CreateService(provider);

            var can = await svc.CanTakeQuizAsync(UserId);

            Assert.True(can);
        }

        [Fact]
        public async Task CanTakeQuizAsync_FreeTierAtLimit_ReturnsFalse()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.UsageTrackings.Add(new UsageTracking
                {
                    UserId = UserId,
                    Month = DateTime.UtcNow.Month,
                    Year = DateTime.UtcNow.Year,
                    QuizzesTaken = 5 // at the free limit of 5
                });
            });
            var svc = CreateService(provider);

            var can = await svc.CanTakeQuizAsync(UserId);

            Assert.False(can);
        }

        [Fact]
        public async Task CanTakeQuizAsync_FreeTierNoUsageRecord_ReturnsTrue()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var can = await svc.CanTakeQuizAsync(UserId);

            Assert.True(can); // 0 quizzes taken < 5 limit
        }

        [Theory]
        [InlineData(SubscriptionTier.Premium)]
        [InlineData(SubscriptionTier.Pro)]
        [InlineData(SubscriptionTier.Enterprise)]
        public async Task CanTakeQuizAsync_PaidTierOverFreeLimit_ReturnsTrue(
            SubscriptionTier tier)
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId, Tier = tier, IsActive = true
                });
                ctx.UsageTrackings.Add(new UsageTracking
                {
                    UserId = UserId,
                    Month = DateTime.UtcNow.Month,
                    Year = DateTime.UtcNow.Year,
                    QuizzesTaken = 1000 // far over free limit
                });
            });
            var svc = CreateService(provider);

            var can = await svc.CanTakeQuizAsync(UserId);

            Assert.True(can); // paid tiers are unlimited
        }

        // ── IncrementQuizUsageAsync ─────────────────────────────────────────────

        [Fact]
        public async Task IncrementQuizUsageAsync_NoExistingRecord_CreatesWithCount1()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            await svc.IncrementQuizUsageAsync(UserId);

            var count = await svc.GetMonthlyQuizCountAsync(UserId);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task IncrementQuizUsageAsync_ExistingRecord_IncrementsCount()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.UsageTrackings.Add(new UsageTracking
                {
                    UserId = UserId,
                    Month = DateTime.UtcNow.Month,
                    Year = DateTime.UtcNow.Year,
                    QuizzesTaken = 3
                });
            });
            var svc = CreateService(provider);

            await svc.IncrementQuizUsageAsync(UserId);

            var count = await svc.GetMonthlyQuizCountAsync(UserId);
            Assert.Equal(4, count);
        }

        [Fact]
        public async Task IncrementQuizUsageAsync_TracksByCurrentMonthAndYear()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                // Last month's record — should be ignored
                ctx.UsageTrackings.Add(new UsageTracking
                {
                    UserId = UserId,
                    Month = DateTime.UtcNow.AddMonths(-1).Month,
                    Year = DateTime.UtcNow.AddMonths(-1).Year,
                    QuizzesTaken = 5
                });
            });
            var svc = CreateService(provider);

            await svc.IncrementQuizUsageAsync(UserId);

            var count = await svc.GetMonthlyQuizCountAsync(UserId);
            Assert.Equal(1, count); // new record for current month
        }

        // ── GetMonthlyQuizCountAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetMonthlyQuizCountAsync_NoRecord_Returns0()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var count = await svc.GetMonthlyQuizCountAsync(UserId);

            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetMonthlyQuizCountAsync_OnlyCountsCurrentMonth()
        {
            var now = DateTime.UtcNow;
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.UsageTrackings.AddRange(
                    new UsageTracking { UserId = UserId, Month = now.Month, Year = now.Year, QuizzesTaken = 3 },
                    new UsageTracking { UserId = UserId, Month = now.AddMonths(-1).Month, Year = now.AddMonths(-1).Year, QuizzesTaken = 99 }
                );
            });
            var svc = CreateService(provider);

            var count = await svc.GetMonthlyQuizCountAsync(UserId);

            Assert.Equal(3, count);
        }

        // ── CreateSubscriptionAsync ─────────────────────────────────────────────

        [Fact]
        public async Task CreateSubscriptionAsync_NewUser_CreatesActiveSubscription()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var success = await svc.CreateSubscriptionAsync(UserId, SubscriptionTier.Premium);

            Assert.True(success);
            var tier = await svc.GetUserTierAsync(UserId);
            Assert.Equal(SubscriptionTier.Premium, tier);
        }

        [Fact]
        public async Task CreateSubscriptionAsync_WithStripeId_PersistsStripeId()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            await svc.CreateSubscriptionAsync(UserId, SubscriptionTier.Pro, "stripe-sub-123");

            var sub = await svc.GetUserSubscriptionAsync(UserId);
            Assert.Equal("stripe-sub-123", sub!.StripeSubscriptionId);
        }

        // ── UpdateSubscriptionAsync ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateSubscriptionAsync_ExistingSubscription_UpdatesTier()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId, Tier = SubscriptionTier.Premium, IsActive = true
                });
            });
            var svc = CreateService(provider);

            var success = await svc.UpdateSubscriptionAsync(UserId, SubscriptionTier.Pro);

            Assert.True(success);
            var tier = await svc.GetUserTierAsync(UserId);
            Assert.Equal(SubscriptionTier.Pro, tier);
        }

        [Fact]
        public async Task UpdateSubscriptionAsync_NoExistingSubscription_CreatesNewOne()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var success = await svc.UpdateSubscriptionAsync(UserId, SubscriptionTier.Enterprise);

            Assert.True(success);
            var tier = await svc.GetUserTierAsync(UserId);
            Assert.Equal(SubscriptionTier.Enterprise, tier);
        }

        // ── CancelSubscriptionAsync ─────────────────────────────────────────────

        [Fact]
        public async Task CancelSubscriptionAsync_ActiveSubscription_DeactivatesIt()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId, Tier = SubscriptionTier.Premium, IsActive = true
                });
            });
            var svc = CreateService(provider);

            var success = await svc.CancelSubscriptionAsync(UserId);

            Assert.True(success);
            // After cancellation, user has no active subscription → defaults to Free
            var tier = await svc.GetUserTierAsync(UserId);
            Assert.Equal(SubscriptionTier.Free, tier);
        }

        [Fact]
        public async Task CancelSubscriptionAsync_ActiveSubscription_SetsEndDate()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId, Tier = SubscriptionTier.Pro, IsActive = true
                });
            });
            var svc = CreateService(provider);

            await svc.CancelSubscriptionAsync(UserId);

            // Verify via a fresh scope
            using var scope = provider.CreateScope();
            var ctx2 = scope.ServiceProvider.GetRequiredService<BlazorQuizApp.Models.ApplicationDbContext>();
            var sub = ctx2.Subscriptions.First(s => s.UserId == UserId);
            Assert.False(sub.IsActive);
            Assert.NotNull(sub.EndDate);
            Assert.True(sub.EndDate >= before);
        }

        [Fact]
        public async Task CancelSubscriptionAsync_NoSubscription_ReturnsFalse()
        {
            using var provider = DbContextHelper.CreateServiceProvider();
            var svc = CreateService(provider);

            var success = await svc.CancelSubscriptionAsync(UserId);

            Assert.False(success);
        }

        [Fact]
        public async Task CancelSubscriptionAsync_AfterCancel_CanTakeQuizRespectsFreeLimitAgain()
        {
            using var provider = DbContextHelper.CreateServiceProvider(seed: ctx =>
            {
                ctx.Subscriptions.Add(new Subscription
                {
                    UserId = UserId, Tier = SubscriptionTier.Premium, IsActive = true
                });
                ctx.UsageTrackings.Add(new UsageTracking
                {
                    UserId = UserId,
                    Month = DateTime.UtcNow.Month,
                    Year = DateTime.UtcNow.Year,
                    QuizzesTaken = 5 // at free limit
                });
            });
            var svc = CreateService(provider);

            await svc.CancelSubscriptionAsync(UserId);
            var can = await svc.CanTakeQuizAsync(UserId);

            Assert.False(can); // back to free tier, at limit
        }
    }
}
