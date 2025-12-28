using System.Text.RegularExpressions;

namespace BlazorQuizApp.Services
{
    /// <summary>
    /// Service for input validation and sanitization
    /// </summary>
    public interface IValidationService
    {
        bool IsValidEmail(string? email);
        bool IsValidQuestionText(string? text);
        bool IsValidOptionText(string? text);
        string SanitizeInput(string? input, int maxLength = 1000);
        bool IsValidCategory(string? category);
        bool IsValidDifficulty(string? difficulty);
    }

    public class ValidationService : IValidationService
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "General", "ASP.NET Core", "C#", "JavaScript", "Database",
            "Web Development", "Programming Concepts", "Blazor", "Entity Framework"
        };

        private static readonly HashSet<string> ValidDifficulties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Easy", "Medium", "Hard"
        };

        public bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email) && email.Length <= 256;
        }

        public bool IsValidQuestionText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var sanitized = SanitizeInput(text, 1000);
            return sanitized.Length >= 10 && sanitized.Length <= 1000;
        }

        public bool IsValidOptionText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var sanitized = SanitizeInput(text, 500);
            return sanitized.Length >= 1 && sanitized.Length <= 500;
        }

        public string SanitizeInput(string? input, int maxLength = 1000)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove potentially dangerous characters but keep basic formatting
            var sanitized = Regex.Replace(input, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            sanitized = Regex.Replace(sanitized, @"<[^>]+>", string.Empty); // Remove HTML tags
            sanitized = sanitized.Trim();

            // Limit length
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
            }

            return sanitized;
        }

        public bool IsValidCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            return ValidCategories.Contains(category) || category.Length <= 100;
        }

        public bool IsValidDifficulty(string? difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
                return false;

            return ValidDifficulties.Contains(difficulty);
        }
    }
}

