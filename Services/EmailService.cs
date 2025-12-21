using BlazorQuizApp.Configuration;
using BlazorQuizApp.Data;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BlazorQuizApp.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailVerificationAsync(string email, string userId, string token);
        Task<bool> SendPasswordResetAsync(string email, string token);
        Task<bool> SendWelcomeEmailAsync(string email, string userName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly AppSettings _appSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IOptions<AppSettings> appSettings,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string userId, string token)
        {
            try
            {
                var verificationUrl = $"{_appSettings.BaseUrl}/Account/ConfirmEmail?userId={userId}&code={Uri.EscapeDataString(token)}";

                var subject = "Verify your email - Quiz App";
                var htmlContent = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background-color: #0d6efd; padding: 20px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>Quiz App</h1>
                        </div>
                        <div style='padding: 30px; background-color: #f8f9fa;'>
                            <h2 style='color: #333;'>Verify Your Email Address</h2>
                            <p style='color: #666; line-height: 1.6;'>
                                Thank you for registering with Quiz App! To complete your registration, 
                                please verify your email address by clicking the button below:
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationUrl}' 
                                   style='background-color: #0d6efd; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Verify Email Address
                                </a>
                            </div>
                            <p style='color: #999; font-size: 12px;'>
                                If you didn't create an account, you can safely ignore this email.
                            </p>
                            <p style='color: #999; font-size: 12px;'>
                                If the button doesn't work, copy and paste this link into your browser:<br>
                                <a href='{verificationUrl}'>{verificationUrl}</a>
                            </p>
                        </div>
                        <div style='background-color: #333; padding: 20px; text-align: center;'>
                            <p style='color: #999; margin: 0; font-size: 12px;'>
                                © {DateTime.UtcNow.Year} Quiz App. All rights reserved.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(string email, string token)
        {
            try
            {
                var resetUrl = $"{_appSettings.BaseUrl}/Account/ResetPassword?code={Uri.EscapeDataString(token)}";

                var subject = "Reset your password - Quiz App";
                var htmlContent = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background-color: #0d6efd; padding: 20px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>Quiz App</h1>
                        </div>
                        <div style='padding: 30px; background-color: #f8f9fa;'>
                            <h2 style='color: #333;'>Reset Your Password</h2>
                            <p style='color: #666; line-height: 1.6;'>
                                We received a request to reset your password. Click the button below to create a new password:
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetUrl}' 
                                   style='background-color: #0d6efd; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Reset Password
                                </a>
                            </div>
                            <p style='color: #999; font-size: 12px;'>
                                If you didn't request a password reset, you can safely ignore this email.
                            </p>
                            <p style='color: #999; font-size: 12px;'>
                                This link will expire in 24 hours.
                            </p>
                        </div>
                        <div style='background-color: #333; padding: 20px; text-align: center;'>
                            <p style='color: #999; margin: 0; font-size: 12px;'>
                                © {DateTime.UtcNow.Year} Quiz App. All rights reserved.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string userName)
        {
            try
            {
                var subject = "Welcome to Quiz App!";
                var htmlContent = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background-color: #0d6efd; padding: 20px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>Quiz App</h1>
                        </div>
                        <div style='padding: 30px; background-color: #f8f9fa;'>
                            <h2 style='color: #333;'>Welcome, {userName}!</h2>
                            <p style='color: #666; line-height: 1.6;'>
                                Thank you for joining Quiz App! You're now ready to test your knowledge 
                                and challenge yourself with our interactive quizzes.
                            </p>
                            <h3 style='color: #333;'>What's Next?</h3>
                            <ul style='color: #666; line-height: 1.8;'>
                                <li>Take your first quiz and see how you score</li>
                                <li>Check out the leaderboard to see top performers</li>
                                <li>Track your progress in the statistics page</li>
                                <li>Upgrade to Premium for unlimited quizzes and advanced features</li>
                            </ul>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{_appSettings.BaseUrl}/quiz' 
                                   style='background-color: #0d6efd; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Start Your First Quiz
                                </a>
                            </div>
                        </div>
                        <div style='background-color: #333; padding: 20px; text-align: center;'>
                            <p style='color: #999; margin: 0; font-size: 12px;'>
                                © {DateTime.UtcNow.Year} Quiz App. All rights reserved.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", email);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                if (string.IsNullOrEmpty(_emailSettings.SendGridApiKey))
                {
                    _logger.LogWarning("SendGrid API key not configured. Email not sent to {Email}", toEmail);
                    return false;
                }

                var client = new SendGridClient(_emailSettings.SendGridApiKey);
                var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send email to {Email}. Status: {StatusCode}",
                        toEmail, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }
    }
}
