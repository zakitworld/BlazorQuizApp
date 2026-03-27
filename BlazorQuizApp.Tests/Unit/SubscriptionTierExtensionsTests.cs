using BlazorQuizApp.Models;
using Xunit;

namespace BlazorQuizApp.Tests.Unit
{
    public class SubscriptionTierExtensionsTests
    {
        // ── GetDisplayName ──────────────────────────────────────────────────────

        [Theory]
        [InlineData(SubscriptionTier.Free, "Free")]
        [InlineData(SubscriptionTier.Premium, "Premium")]
        [InlineData(SubscriptionTier.Pro, "Pro")]
        [InlineData(SubscriptionTier.Enterprise, "Enterprise")]
        public void GetDisplayName_ReturnsExpectedLabel(SubscriptionTier tier, string expected)
        {
            Assert.Equal(expected, tier.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_UnknownTierValue_ReturnsUnknown()
        {
            Assert.Equal("Unknown", ((SubscriptionTier)999).GetDisplayName());
        }

        // ── GetMonthlyPrice ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(SubscriptionTier.Free, 0)]
        [InlineData(SubscriptionTier.Premium, 25)]
        [InlineData(SubscriptionTier.Pro, 75)]
        [InlineData(SubscriptionTier.Enterprise, 250)]
        public void GetMonthlyPrice_ReturnsCorrectGhsPrice(SubscriptionTier tier, decimal expected)
        {
            Assert.Equal(expected, tier.GetMonthlyPrice());
        }

        [Fact]
        public void GetMonthlyPrice_PremiumCostsLessThanPro()
        {
            Assert.True(SubscriptionTier.Premium.GetMonthlyPrice() <
                        SubscriptionTier.Pro.GetMonthlyPrice());
        }

        [Fact]
        public void GetMonthlyPrice_ProCostsLessThanEnterprise()
        {
            Assert.True(SubscriptionTier.Pro.GetMonthlyPrice() <
                        SubscriptionTier.Enterprise.GetMonthlyPrice());
        }

        // ── GetMonthlyQuizLimit ─────────────────────────────────────────────────

        [Fact]
        public void GetMonthlyQuizLimit_Free_Returns5()
        {
            Assert.Equal(5, SubscriptionTier.Free.GetMonthlyQuizLimit());
        }

        [Theory]
        [InlineData(SubscriptionTier.Premium)]
        [InlineData(SubscriptionTier.Pro)]
        [InlineData(SubscriptionTier.Enterprise)]
        public void GetMonthlyQuizLimit_PaidTiers_ReturnsIntMaxValue(SubscriptionTier tier)
        {
            Assert.Equal(int.MaxValue, tier.GetMonthlyQuizLimit());
        }

        [Fact]
        public void GetMonthlyQuizLimit_PaidTierIsUnlimited_CanAlwaysTakeMoreQuizzes()
        {
            // Simulate usage check: any count < int.MaxValue is "under limit"
            const int usage = 10_000;
            Assert.True(usage < SubscriptionTier.Premium.GetMonthlyQuizLimit());
        }

        // ── GetFeatures ─────────────────────────────────────────────────────────

        [Fact]
        public void GetFeatures_Free_ContainsExpectedFeatures()
        {
            var features = SubscriptionTier.Free.GetFeatures();
            Assert.NotEmpty(features);
            Assert.Contains(features, f => f.Contains("5 quizzes"));
        }

        [Fact]
        public void GetFeatures_Premium_ContainsUnlimitedQuizzes()
        {
            var features = SubscriptionTier.Premium.GetFeatures();
            Assert.Contains(features, f => f.Contains("Unlimited"));
        }

        [Fact]
        public void GetFeatures_Pro_ContainsEverythingInPremium()
        {
            var features = SubscriptionTier.Pro.GetFeatures();
            Assert.Contains(features, f => f.Contains("Premium"));
        }

        [Fact]
        public void GetFeatures_Enterprise_ContainsEverythingInPro()
        {
            var features = SubscriptionTier.Enterprise.GetFeatures();
            Assert.Contains(features, f => f.Contains("Pro"));
        }

        [Fact]
        public void GetFeatures_HigherTiersHaveMoreOrEqualFeatures()
        {
            Assert.True(SubscriptionTier.Premium.GetFeatures().Count >=
                        SubscriptionTier.Free.GetFeatures().Count);
        }
    }
}
