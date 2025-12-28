using BlazorQuizApp.Configuration;
using BlazorQuizApp.Models;
using Microsoft.Extensions.Options;
using PayStack.Net;
using System.Security.Cryptography;
using System.Text;

namespace BlazorQuizApp.Services
{
    public interface IPaystackPaymentService
    {
        Task<string> InitializePaymentAsync(string userId, string userEmail, SubscriptionTier tier);
        Task<bool> VerifyPaymentAsync(string reference);
        Task<bool> HandleWebhookAsync(string payload, string signature);
    }

    public class PaystackPaymentService : IPaystackPaymentService
    {
        private readonly PaystackSettings _paystackSettings;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<PaystackPaymentService> _logger;
        private readonly PayStackApi _paystackApi;
        private readonly string _baseUrl;

        public PaystackPaymentService(
            IOptions<PaystackSettings> paystackSettings,
            IOptions<AppSettings> appSettings,
            ISubscriptionService subscriptionService,
            ILogger<PaystackPaymentService> logger)
        {
            _paystackSettings = paystackSettings.Value;
            _subscriptionService = subscriptionService;
            _logger = logger;
            _baseUrl = string.IsNullOrWhiteSpace(appSettings.Value.BaseUrl)
                ? "https://localhost:7233"
                : appSettings.Value.BaseUrl.TrimEnd('/');
            _paystackApi = new PayStackApi(_paystackSettings.SecretKey);
        }

        public async Task<string> InitializePaymentAsync(string userId, string userEmail, SubscriptionTier tier)
        {
            try
            {
                var amount = GetTierAmount(tier);
                var planCode = tier switch
                {
                    SubscriptionTier.Premium => _paystackSettings.PricingPlans.PremiumMonthly,
                    SubscriptionTier.Pro => _paystackSettings.PricingPlans.ProMonthly,
                    _ => throw new ArgumentException("Invalid subscription tier for payment")
                };

                // Initialize transaction
                var metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "tier", tier.ToString() },
                    { "planCode", planCode }
                });

                var request = new TransactionInitializeRequest
                {
                    AmountInKobo = amount * 100, // Convert to kobo (GHS cents)
                    Email = userEmail,
                    Reference = GenerateReference(userId),
                    Currency = "GHS", // Ghana Cedis
                    CallbackUrl = $"{_baseUrl}/subscription/verify",
                    Metadata = metadata
                };

                var response = _paystackApi.Transactions.Initialize(request);

                if (response.Status)
                {
                    _logger.LogInformation("Paystack payment initialized for user {UserId}, tier {Tier}, reference {Reference}",
                        userId, tier, request.Reference);
                    return response.Data.AuthorizationUrl;
                }
                else
                {
                    _logger.LogError("Failed to initialize Paystack payment: {Message}", response.Message);
                    throw new Exception($"Payment initialization failed: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Paystack payment for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            try
            {
                var response = _paystackApi.Transactions.Verify(reference);

                if (response.Status && response.Data.Status == "success")
                {
                    var metadata = response.Data.Metadata;
                    var userId = metadata["userId"]?.ToString() ?? string.Empty;
                    var tierString = metadata["tier"]?.ToString() ?? string.Empty;

                    if (Enum.TryParse<SubscriptionTier>(tierString, out var tier))
                    {
                        await _subscriptionService.CreateSubscriptionAsync(userId, tier, reference);
                        _logger.LogInformation("Payment verified and subscription activated for user {UserId}, tier {Tier}", userId, tier);
                        return true;
                    }
                }

                _logger.LogWarning("Payment verification failed for reference {Reference}", reference);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment for reference {Reference}", reference);
                return false;
            }
        }

        public async Task<bool> HandleWebhookAsync(string payload, string signature)
        {
            try
            {
                // Verify webhook signature
                if (!VerifyWebhookSignature(payload, signature))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return false;
                }

                var webhookData = System.Text.Json.JsonSerializer.Deserialize<PaystackWebhook>(payload);
                if (webhookData == null) return false;

                _logger.LogInformation("Processing Paystack webhook: {Event}", webhookData.Event);

                switch (webhookData.Event)
                {
                    case "charge.success":
                        await HandleChargeSuccessAsync(webhookData);
                        break;

                    case "subscription.create":
                        await HandleSubscriptionCreateAsync(webhookData);
                        break;

                    case "subscription.disable":
                        await HandleSubscriptionDisableAsync(webhookData);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook event: {Event}", webhookData.Event);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Paystack webhook");
                return false;
            }
        }

        private async Task HandleChargeSuccessAsync(PaystackWebhook webhook)
        {
            var data = webhook.Data;
            var reference = data.Reference;

            await VerifyPaymentAsync(reference);
        }

        private async Task HandleSubscriptionCreateAsync(PaystackWebhook webhook)
        {
            _logger.LogInformation("Subscription created via webhook");
            // Additional subscription handling if needed
            await Task.CompletedTask;
        }

        private async Task HandleSubscriptionDisableAsync(PaystackWebhook webhook)
        {
            var data = webhook.Data;
            var customerCode = data.Customer?.CustomerCode;

            if (!string.IsNullOrEmpty(customerCode))
            {
                // Find and cancel subscription
                _logger.LogInformation("Subscription disabled for customer {CustomerCode}", customerCode);
            }

            await Task.CompletedTask;
        }

        private bool VerifyWebhookSignature(string payload, string signature)
        {
            var hash = ComputeHash(payload, _paystackSettings.SecretKey);
            return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHash(string payload, string secret)
        {
            var encoding = new UTF8Encoding();
            var keyBytes = encoding.GetBytes(secret);
            var messageBytes = encoding.GetBytes(payload);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private string GenerateReference(string userId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"QZ-{userId.Substring(0, Math.Min(8, userId.Length))}-{timestamp}";
        }

        private int GetTierAmount(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Premium => 25,  // GHS 25.00
                SubscriptionTier.Pro => 75,      // GHS 75.00
                SubscriptionTier.Enterprise => 250, // GHS 250.00
                _ => 0
            };
        }
    }

    // Webhook models
    public class PaystackWebhook
    {
        public string Event { get; set; } = string.Empty;
        public PaystackWebhookData Data { get; set; } = new();
    }

    public class PaystackWebhookData
    {
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public PaystackCustomer? Customer { get; set; }
    }

    public class PaystackCustomer
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
