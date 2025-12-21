using BlazorQuizApp.Configuration;
using BlazorQuizApp.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BlazorQuizApp.Services
{
    public interface IStripePaymentService
    {
        Task<string> CreateCheckoutSessionAsync(string userId, string userEmail, SubscriptionTier tier);
        Task<bool> HandleWebhookAsync(string json, string signature);
    }

    public class StripePaymentService : IStripePaymentService
    {
        private readonly StripeSettings _stripeSettings;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly string _baseUrl;

        public StripePaymentService(
            IOptions<StripeSettings> stripeSettings,
            IOptions<AppSettings> appSettings,
            ISubscriptionService subscriptionService,
            ILogger<StripePaymentService> logger)
        {
            _stripeSettings = stripeSettings.Value;
            _subscriptionService = subscriptionService;
            _logger = logger;
            _baseUrl = appSettings.Value.BaseUrl;

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<string> CreateCheckoutSessionAsync(string userId, string userEmail, SubscriptionTier tier)
        {
            try
            {
                var priceId = tier switch
                {
                    SubscriptionTier.Premium => _stripeSettings.PricingIds.PremiumMonthly,
                    SubscriptionTier.Pro => _stripeSettings.PricingIds.ProMonthly,
                    _ => throw new ArgumentException("Invalid subscription tier for checkout")
                };

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = priceId,
                            Quantity = 1,
                        }
                    },
                    Mode = "subscription",
                    SuccessUrl = $"{_baseUrl}/subscription/success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{_baseUrl}/pricing",
                    ClientReferenceId = userId,
                    CustomerEmail = userEmail,
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "tier", tier.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation("Created Stripe checkout session for user {UserId}, tier {Tier}", userId, tier);
                return session.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe checkout session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HandleWebhookAsync(string json, string signature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    _stripeSettings.WebhookSecret
                );

                _logger.LogInformation("Processing Stripe webhook: {EventType}", stripeEvent.Type);

                switch (stripeEvent.Type)
                {
                    case Events.CheckoutSessionCompleted:
                        await HandleCheckoutSessionCompletedAsync(stripeEvent);
                        break;

                    case Events.CustomerSubscriptionUpdated:
                        await HandleSubscriptionUpdatedAsync(stripeEvent);
                        break;

                    case Events.CustomerSubscriptionDeleted:
                        await HandleSubscriptionDeletedAsync(stripeEvent);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                        break;
                }

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return false;
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            var userId = session.ClientReferenceId;
            var tierString = session.Metadata.GetValueOrDefault("tier");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tierString))
            {
                _logger.LogWarning("Missing userId or tier in checkout session metadata");
                return;
            }

            if (Enum.TryParse<SubscriptionTier>(tierString, out var tier))
            {
                await _subscriptionService.CreateSubscriptionAsync(
                    userId,
                    tier,
                    session.SubscriptionId
                );

                _logger.LogInformation("Activated {Tier} subscription for user {UserId}", tier, userId);
            }
        }

        private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            // Handle subscription updates (e.g., plan changes)
            _logger.LogInformation("Subscription updated: {SubscriptionId}", subscription.Id);
        }

        private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            // Find user by Stripe subscription ID and cancel their subscription
            _logger.LogInformation("Subscription deleted: {SubscriptionId}", subscription.Id);
        }
    }
}
