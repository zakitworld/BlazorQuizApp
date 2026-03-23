using BlazorQuizApp.Models;

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

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            _logger.LogInformation("Sending email to {Email}: {Subject}", email, subject);
            // In a real app, integrate with SendGrid or SMTP here
            await Task.CompletedTask;
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
