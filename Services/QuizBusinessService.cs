using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace BlazorQuizApp.Services
{
    public class QuizBusinessService : IQuizBusinessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuizBusinessService> _logger;
        private readonly ICacheService? _cacheService;
        private readonly IValidationService? _validationService;
        private readonly IAuditService? _auditService;

        public QuizBusinessService(
            ApplicationDbContext context,
            ILogger<QuizBusinessService> logger,
            ICacheService? cacheService = null,
            IValidationService? validationService = null,
            IAuditService? auditService = null)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
            _validationService = validationService;
            _auditService = auditService;
        }

        public async Task<List<QuizQuestion>> GetQuestionsAsync()
        {
            const string cacheKey = "quiz_questions_all";

            if (_cacheService != null)
            {
                return await _cacheService.GetOrSetAsync(cacheKey, FetchQuestionsFromDatabaseAsync,
                    TimeSpan.FromMinutes(10)) ?? [];
            }

            return await FetchQuestionsFromDatabaseAsync();
        }

        private async Task<List<QuizQuestion>> FetchQuestionsFromDatabaseAsync()
        {
            return await _context.QuizQuestions
                .AsNoTracking()
                .Include(q => q.Options)
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<QuizQuestion?> GetQuestionByIdAsync(int id)
        {
            return await _context.QuizQuestions
                .AsNoTracking()
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> AddQuestionAsync(NewQuestionForm questionForm)
        {
            try
            {
                if (!await ValidateQuestionFormAsync(questionForm))
                    return false;

                var sanitizedText = _validationService?.SanitizeInput(questionForm.QuestionText, 1000)
                    ?? questionForm.QuestionText.Trim();
                var sanitizedCategory = _validationService?.SanitizeInput(questionForm.Category, 100)
                    ?? questionForm.Category.Trim();
                var sanitizedExplanation = string.IsNullOrWhiteSpace(questionForm.Explanation) ? null
                    : (_validationService?.SanitizeInput(questionForm.Explanation, 2000) ?? questionForm.Explanation.Trim());

                var question = new QuizQuestion
                {
                    QuestionText = sanitizedText,
                    Category = sanitizedCategory,
                    Difficulty = questionForm.Difficulty,
                    Explanation = sanitizedExplanation,
                    CorrectOptionId = 0,
                    Type = questionForm.Type,
                    AcceptedAnswersJson = SerializeAcceptedAnswers(questionForm.AcceptedAnswers),
                    CreatedAt = DateTime.UtcNow,
                    Options = []
                };

                for (int i = 0; question.Type != QuestionType.ShortAnswer && i < questionForm.OptionTexts.Count; i++)
                {
                    var optionText = questionForm.OptionTexts[i];
                    if (!string.IsNullOrWhiteSpace(optionText))
                    {
                        var sanitized = _validationService?.SanitizeInput(optionText, 500) ?? optionText.Trim();
                        question.Options.Add(new QuizOption { Text = sanitized });
                    }
                }

                _context.QuizQuestions.Add(question);
                await _context.SaveChangesAsync();
                _cacheService?.Remove("quiz_questions_all");

                if (questionForm.CorrectOptionIndex >= 0 && questionForm.CorrectOptionIndex < question.Options.Count)
                {
                    question.CorrectOptionId = question.Options[questionForm.CorrectOptionIndex].Id;
                }
                question.CorrectOptionIdsJson = JsonSerializer.Serialize(questionForm.CorrectOptionIndexes
                    .Where(i => i >= 0 && i < question.Options.Count).Select(i => question.Options[i].Id));
                await AddVersionAsync(question);
                await _context.SaveChangesAsync();

                if (_auditService != null)
                    await _auditService.WriteAsync("QuestionCreated", nameof(QuizQuestion), question.Id, question.Category);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question");
                return false;
            }
        }

        public async Task<bool> UpdateQuestionAsync(int id, NewQuestionForm questionForm)
        {
            try
            {
                if (!await ValidateQuestionFormAsync(questionForm))
                    return false;

                var question = await _context.QuizQuestions
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (question == null) return false;

                question.QuestionText = _validationService?.SanitizeInput(questionForm.QuestionText, 1000)
                    ?? questionForm.QuestionText.Trim();
                question.Category = _validationService?.SanitizeInput(questionForm.Category, 100)
                    ?? questionForm.Category.Trim();
                question.Difficulty = questionForm.Difficulty;
                question.Explanation = string.IsNullOrWhiteSpace(questionForm.Explanation) ? null
                    : (_validationService?.SanitizeInput(questionForm.Explanation, 2000) ?? questionForm.Explanation.Trim());
                question.CorrectOptionId = 0;
                question.Type = questionForm.Type;
                question.AcceptedAnswersJson = SerializeAcceptedAnswers(questionForm.AcceptedAnswers);
                question.CurrentVersion++;
                question.UpdatedAt = DateTime.UtcNow;

                _context.QuizOptions.RemoveRange(question.Options);
                question.Options.Clear();

                for (int i = 0; question.Type != QuestionType.ShortAnswer && i < questionForm.OptionTexts.Count; i++)
                {
                    var optionText = questionForm.OptionTexts[i];
                    if (!string.IsNullOrWhiteSpace(optionText))
                    {
                        var sanitized = _validationService?.SanitizeInput(optionText, 500) ?? optionText.Trim();
                        question.Options.Add(new QuizOption { Text = sanitized });
                    }
                }

                await _context.SaveChangesAsync();

                if (questionForm.CorrectOptionIndex >= 0 && questionForm.CorrectOptionIndex < question.Options.Count)
                {
                    question.CorrectOptionId = question.Options[questionForm.CorrectOptionIndex].Id;
                }
                question.CorrectOptionIdsJson = JsonSerializer.Serialize(questionForm.CorrectOptionIndexes
                    .Where(i => i >= 0 && i < question.Options.Count).Select(i => question.Options[i].Id));
                await AddVersionAsync(question);
                await _context.SaveChangesAsync();

                _cacheService?.Remove("quiz_questions_all");
                if (_auditService != null)
                    await _auditService.WriteAsync("QuestionUpdated", nameof(QuizQuestion), id, question.Category);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            try
            {
                var question = await _context.QuizQuestions
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (question == null) return false;

                _context.QuizQuestions.Remove(question);
                await _context.SaveChangesAsync();
                _cacheService?.Clear();
                if (_auditService != null)
                    await _auditService.WriteAsync("QuestionDeleted", nameof(QuizQuestion), id, question.Category);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", id);
                return false;
            }
        }

        public async Task<QuizResult> SubmitQuizAsync(
            List<QuizAnswer> answers, string userId, TimeSpan duration,
            string category, bool isPracticeMode, int score, int? assignmentId = null,
            int attemptNumber = 0, int passPercentage = 0)
        {
            try
            {
                if (!await ValidateQuizSubmissionAsync(answers))
                    throw new ValidationException("Invalid quiz submission");

                var user = await _context.Users.FindAsync(userId);
                var userName = user?.Email ?? "Anonymous";

                var questions = await GetQuestionsAsync();
                var correctAnswers = 0;

                foreach (var answer in answers)
                {
                    var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question == null) continue;
                    answer.IsCorrect = IsAnswerCorrect(question, answer);
                    answer.QuestionVersionNumber = question.CurrentVersion;
                    answer.QuestionTextSnapshot = question.QuestionText;
                    answer.SelectedAnswerSnapshot = GetSelectedAnswerSnapshot(question, answer);
                    answer.CorrectAnswerSnapshot = GetCorrectAnswerSnapshot(question);
                    if (answer.IsCorrect) correctAnswers++;
                }

                var result = new QuizResult
                {
                    UserId = userId,
                    UserName = userName,
                    Category = category,
                    TotalQuestions = answers.Count,
                    CorrectAnswers = correctAnswers,
                    // Never trust the score supplied by the browser. Award one point per
                    // answer verified against the server-side question data.
                    Score = correctAnswers,
                    IsPracticeMode = isPracticeMode,
                    QuizAssignmentId = assignmentId,
                    AttemptNumber = attemptNumber,
                    CompletedAt = DateTime.UtcNow,
                    Duration = duration,
                    Answers = answers
                };
                result.IsPassed = passPercentage > 0 && result.ScorePercentage >= passPercentage;
                if (assignmentId.HasValue && result.IsPassed)
                    result.CertificateCode = $"QZ-{Guid.NewGuid():N}"[..35].ToUpperInvariant();

                // Only persist scored (non-practice) results
                if (!isPracticeMode)
                {
                    _context.QuizResults.Add(result);
                    await _context.SaveChangesAsync();
                    _cacheService?.Clear();
                }

                _logger.LogInformation(
                    "Quiz submitted for user {UserId} — score {Score}%, {Points} pts, practice={Practice}",
                    userId, result.ScorePercentage, score, isPracticeMode);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz for user {UserId}", userId);
                throw;
            }
        }

        public async Task<QuizResult?> GetQuizResultByIdAsync(int resultId, string userId)
        {
            try
            {
                return await _context.QuizResults
                    .Include(r => r.Answers)
                    .FirstOrDefaultAsync(r => r.Id == resultId && r.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quiz result {ResultId}", resultId);
                throw;
            }
        }

        public async Task<List<QuizResult>> GetUserQuizHistoryAsync(string userId)
        {
            try
            {
                return await _context.QuizResults
                    .Where(r => r.UserId == userId && !r.IsPracticeMode)
                    .Include(r => r.Answers)
                    .OrderByDescending(r => r.CompletedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quiz history for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<QuizResult>> GetLeaderboardAsync(int topCount = 10, string? category = null)
        {
            var cacheKey = $"leaderboard_{category ?? "all"}_{topCount}";

            if (_cacheService != null)
            {
                return await _cacheService.GetOrSetAsync(cacheKey,
                    () => FetchLeaderboardAsync(topCount, category),
                    TimeSpan.FromMinutes(5)) ?? [];
            }

            return await FetchLeaderboardAsync(topCount, category);
        }

        private async Task<List<QuizResult>> FetchLeaderboardAsync(int topCount, string? category)
        {
            try
            {
                var query = _context.QuizResults
                    .Where(r => !r.IsPracticeMode);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(r => r.Category == category);

                return await query
                    .OrderByDescending(r => r.Score)
                    .ThenByDescending(r => (double)r.CorrectAnswers / r.TotalQuestions)
                    .ThenBy(r => r.Duration)
                    .Take(topCount)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leaderboard");
                throw;
            }
        }

        public async Task<List<CategoryStat>> GetCategoryStatsAsync(string userId)
        {
            try
            {
                var results = await _context.QuizResults
                    .Where(r => r.UserId == userId && !r.IsPracticeMode && r.Category != string.Empty)
                    .ToListAsync();

                return results
                    .GroupBy(r => r.Category)
                    .Select(g => new CategoryStat(
                        Category: g.Key,
                        QuizzesTaken: g.Count(),
                        BestScore: g.Max(r => r.ScorePercentage),
                        AverageScore: g.Average(r => r.ScorePercentage),
                        TotalPoints: g.Sum(r => r.Score),
                        BestStreak: 0))
                    .OrderByDescending(s => s.AverageScore)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category stats for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<string, double>> GetBestScoresPerCategoryAsync(string userId)
        {
            try
            {
                var results = await _context.QuizResults
                    .Where(r => r.UserId == userId && !r.IsPracticeMode && r.Category != string.Empty)
                    .ToListAsync();

                return results
                    .GroupBy(r => r.Category)
                    .ToDictionary(g => g.Key, g => g.Max(r => r.ScorePercentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving best scores for user {UserId}", userId);
                return [];
            }
        }

        public async Task<bool> ValidateQuizSubmissionAsync(List<QuizAnswer> answers)
        {
            try
            {
                if (answers == null || answers.Count == 0)
                    return false;

                var questions = await GetQuestionsAsync();
                var questionIds = questions.Select(q => q.Id).ToHashSet();

                foreach (var answer in answers)
                {
                    if (!questionIds.Contains(answer.QuestionId))
                        return false;

                    if (answer.SelectedOptionId == -1)
                        continue;

                    var question = questions.First(q => q.Id == answer.QuestionId);
                    if (question.Type == QuestionType.ShortAnswer)
                    {
                        if (answer.TextAnswer?.Length > 1000) return false;
                    }
                    else
                    {
                        var selectedIds = GetSelectedIds(answer);
                        if (selectedIds.Any(id => !question.Options.Select(o => o.Id).Contains(id))) return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating quiz submission");
                throw;
            }
        }

        private async Task<bool> ValidateQuestionFormAsync(NewQuestionForm questionForm)
        {
            try
            {
                if (_validationService != null)
                {
                    if (!_validationService.IsValidQuestionText(questionForm.QuestionText)) return false;
                    if (!_validationService.IsValidCategory(questionForm.Category)) return false;
                    if (!_validationService.IsValidDifficulty(questionForm.Difficulty)) return false;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(questionForm.QuestionText) || questionForm.QuestionText.Length < 10)
                        return false;
                }

                if (questionForm.Type == QuestionType.ShortAnswer)
                    return !string.IsNullOrWhiteSpace(questionForm.AcceptedAnswers);

                if (questionForm.Type == QuestionType.MultipleSelect && questionForm.CorrectOptionIndexes.Count == 0)
                    return false;

                if (questionForm.OptionTexts == null || questionForm.OptionTexts.Count < 2) return false;

                foreach (var optionText in questionForm.OptionTexts)
                {
                    if (_validationService != null)
                    {
                        if (!_validationService.IsValidOptionText(optionText)) return false;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(optionText)) return false;
                    }
                }

                if (questionForm.CorrectOptionIndex < 0 || questionForm.CorrectOptionIndex >= questionForm.OptionTexts.Count)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating question form");
                return false;
            }
        }

        private async Task AddVersionAsync(QuizQuestion question)
        {
            _context.QuestionVersions.Add(new QuestionVersion
            {
                QuizQuestionId = question.Id,
                VersionNumber = question.CurrentVersion,
                QuestionText = question.QuestionText,
                Type = question.Type,
                OptionsJson = JsonSerializer.Serialize(question.Options.Select(o => new { o.Id, o.Text })),
                CorrectAnswersJson = question.Type == QuestionType.ShortAnswer
                    ? question.AcceptedAnswersJson : question.CorrectOptionIdsJson,
                Explanation = question.Explanation
            });
            await Task.CompletedTask;
        }

        public static bool IsAnswerCorrect(QuizQuestion question, QuizAnswer answer)
        {
            if (question.Type == QuestionType.ShortAnswer)
            {
                var accepted = DeserializeStrings(question.AcceptedAnswersJson);
                var normalized = NormalizeAnswer(answer.TextAnswer);
                return normalized.Length > 0 && accepted.Any(x => NormalizeAnswer(x) == normalized);
            }
            if (question.Type == QuestionType.MultipleSelect)
            {
                var selected = GetSelectedIds(answer).Order().ToArray();
                var correct = DeserializeInts(question.CorrectOptionIdsJson).Order().ToArray();
                return selected.Length > 0 && selected.SequenceEqual(correct);
            }
            return answer.SelectedOptionId == question.CorrectOptionId;
        }

        private static List<int> GetSelectedIds(QuizAnswer answer)
        {
            if (!string.IsNullOrWhiteSpace(answer.SelectedOptionIdsJson))
            {
                try { return JsonSerializer.Deserialize<List<int>>(answer.SelectedOptionIdsJson) ?? []; }
                catch (JsonException) { return []; }
            }
            return answer.SelectedOptionId > 0 ? [answer.SelectedOptionId] : [];
        }

        private static string GetSelectedAnswerSnapshot(QuizQuestion question, QuizAnswer answer) =>
            question.Type == QuestionType.ShortAnswer ? answer.TextAnswer ?? string.Empty :
            string.Join("; ", question.Options.Where(o => GetSelectedIds(answer).Contains(o.Id)).Select(o => o.Text));

        private static string GetCorrectAnswerSnapshot(QuizQuestion question) =>
            question.Type == QuestionType.ShortAnswer ? string.Join("; ", DeserializeStrings(question.AcceptedAnswersJson)) :
            string.Join("; ", question.Options.Where(o => o.Id == question.CorrectOptionId ||
                DeserializeInts(question.CorrectOptionIdsJson).Contains(o.Id)).Select(o => o.Text));

        private static string? SerializeAcceptedAnswers(string? value) => string.IsNullOrWhiteSpace(value) ? null :
            JsonSerializer.Serialize(value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        private static List<string> DeserializeStrings(string? json) { try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? []; } catch { return []; } }
        private static List<int> DeserializeInts(string? json) { try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? []; } catch { return []; } }
        private static string NormalizeAnswer(string? value) => Regex.Replace(value?.Trim().ToLowerInvariant() ?? string.Empty, @"[^\p{L}\p{N}\s]", string.Empty);
    }
}
