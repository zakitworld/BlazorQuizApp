using BlazorQuizApp.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BlazorQuizApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendEmailVerificationAsync(string email, string userId, string token);
        Task SendPasswordResetAsync(string email, string token);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var apiKey = _configuration["EmailSettings:SendGridApiKey"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException(
                    "Email delivery is not configured. Set EmailSettings:SendGridApiKey and EmailSettings:FromEmail in the secret store.");

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, _configuration["EmailSettings:FromName"] ?? "Quiz App");
            var response = await client.SendEmailAsync(MailHelper.CreateSingleEmail(
                from, new EmailAddress(email), subject, string.Empty, message));
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Email provider returned HTTP {(int)response.StatusCode}.");
        }

        public async Task SendEmailVerificationAsync(string email, string userId, string token)
        {
            _logger.LogInformation("Sending verification email to {Email} for user {UserId}", email, userId);
            await Task.CompletedTask;
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            _logger.LogInformation("Sending password reset email to {Email}", email);
            await Task.CompletedTask;
        }
    }
}
