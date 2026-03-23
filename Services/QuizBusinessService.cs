using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BlazorQuizApp.Services
{
    /// <summary>
    /// Business service for quiz operations including CRUD operations for questions,
    /// quiz submission, and result retrieval with caching support.
    /// </summary>
    public class QuizBusinessService : IQuizBusinessService
    {
        private readonly ApplicationDbContext _context;
        private readonly QuizService _quizService;
        private readonly ILogger<QuizBusinessService> _logger;
        private readonly ICacheService? _cacheService;
        private readonly IValidationService? _validationService;

        public QuizBusinessService(
            ApplicationDbContext context, 
            QuizService quizService, 
            ILogger<QuizBusinessService> logger,
            ICacheService? cacheService = null,
            IValidationService? validationService = null)
        {
            _context = context;
            _quizService = quizService;
            _logger = logger;
            _cacheService = cacheService;
            _validationService = validationService;
        }

        /// <summary>
        /// Retrieves all quiz questions from the database with caching support.
        /// Falls back to JSON file if database access fails.
        /// </summary>
        /// <returns>List of quiz questions</returns>
        public async Task<List<QuizQuestion>> GetQuestionsAsync()
        {
            const string cacheKey = "quiz_questions_all";
            
            if (_cacheService != null)
            {
                return await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await FetchQuestionsFromDatabaseAsync();
                }, TimeSpan.FromMinutes(10)) ?? new List<QuizQuestion>();
            }

            return await FetchQuestionsFromDatabaseAsync();
        }

        private async Task<List<QuizQuestion>> FetchQuestionsFromDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving quiz questions from database");
                return await _context.QuizQuestions
                    .Include(q => q.Options)
                    .OrderBy(q => q.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quiz questions from database, falling back to JSON file");
                // Fallback to JSON file if database fails
                return await Task.FromResult(_quizService.Questions);
            }
        }

        /// <summary>
        /// Retrieves a specific question by ID from the database.
        /// Falls back to JSON file if database access fails.
        /// </summary>
        /// <param name="id">The question ID</param>
        /// <returns>The quiz question or null if not found</returns>
        public async Task<QuizQuestion?> GetQuestionByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving question with ID: {QuestionId}", id);
                return await _context.QuizQuestions
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving question with ID: {QuestionId} from database, falling back to JSON file", id);
                // Fallback to JSON file if database fails
                return await Task.FromResult(_quizService.GetQuestionById(id));
            }
        }

        /// <summary>
        /// Adds a new question to the database after validation and sanitization.
        /// Invalidates the questions cache upon successful addition.
        /// </summary>
        /// <param name="questionForm">The question form data</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> AddQuestionAsync(NewQuestionForm questionForm)
        {
            try
            {
                if (!await ValidateQuestionFormAsync(questionForm))
                {
                    return false;
                }

                _logger.LogInformation("Adding new question: {QuestionText}", questionForm.QuestionText);
                
                // Sanitize inputs
                var sanitizedQuestionText = _validationService?.SanitizeInput(questionForm.QuestionText, 1000) 
                    ?? questionForm.QuestionText.Trim();
                var sanitizedCategory = _validationService?.SanitizeInput(questionForm.Category, 100) 
                    ?? questionForm.Category.Trim();

                var question = new QuizQuestion
                {
                    QuestionText = sanitizedQuestionText,
                    Category = sanitizedCategory,
                    Difficulty = questionForm.Difficulty,
                    CorrectOptionId = 0, // Temporary value, will be set after saving
                    CreatedAt = DateTime.UtcNow,
                    Options = new List<QuizOption>()
                };

                for (int i = 0; i < questionForm.OptionTexts.Count; i++)
                {
                    var optionText = questionForm.OptionTexts[i];
                    if (!string.IsNullOrWhiteSpace(optionText))
                    {
                        var sanitizedOption = _validationService?.SanitizeInput(optionText, 500) 
                            ?? optionText.Trim();
                        question.Options.Add(new QuizOption { Text = sanitizedOption });
                    }
                }

                _context.QuizQuestions.Add(question);
                await _context.SaveChangesAsync();
                
                // Invalidate cache
                _cacheService?.Remove("quiz_questions_all");

                // Now set the correct option ID using the actual database ID
                if (questionForm.CorrectOptionIndex >= 0 && questionForm.CorrectOptionIndex < question.Options.Count)
                {
                    question.CorrectOptionId = question.Options[questionForm.CorrectOptionIndex].Id;
                    await _context.SaveChangesAsync();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question: {QuestionText}", questionForm.QuestionText);
                return false;
            }
        }

        public async Task<bool> UpdateQuestionAsync(int id, NewQuestionForm questionForm)
        {
            try
            {
                if (!await ValidateQuestionFormAsync(questionForm))
                {
                    return false;
                }

                _logger.LogInformation("Updating question with ID: {QuestionId}", id);
                
                var question = await _context.QuizQuestions
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == id);
                
                if (question == null)
                {
                    _logger.LogWarning("Question with ID {QuestionId} not found", id);
                    return false;
                }

                // Sanitize inputs
                var sanitizedQuestionText = _validationService?.SanitizeInput(questionForm.QuestionText, 1000) 
                    ?? questionForm.QuestionText.Trim();
                var sanitizedCategory = _validationService?.SanitizeInput(questionForm.Category, 100) 
                    ?? questionForm.Category.Trim();

                question.QuestionText = sanitizedQuestionText;
                question.Category = sanitizedCategory;
                question.Difficulty = questionForm.Difficulty;
                question.CorrectOptionId = 0; // Temporary value, will be set after saving
                question.UpdatedAt = DateTime.UtcNow;

                // Remove existing options
                _context.QuizOptions.RemoveRange(question.Options);
                question.Options.Clear();

                // Add new options
                for (int i = 0; i < questionForm.OptionTexts.Count; i++)
                {
                    var optionText = questionForm.OptionTexts[i];
                    if (!string.IsNullOrWhiteSpace(optionText))
                    {
                        var sanitizedOption = _validationService?.SanitizeInput(optionText, 500) 
                            ?? optionText.Trim();
                        question.Options.Add(new QuizOption { Text = sanitizedOption });
                    }
                }

                await _context.SaveChangesAsync();

                // Now set the correct option ID using the actual database ID
                if (questionForm.CorrectOptionIndex >= 0 && questionForm.CorrectOptionIndex < question.Options.Count)
                {
                    question.CorrectOptionId = question.Options[questionForm.CorrectOptionIndex].Id;
                    await _context.SaveChangesAsync();
                }
                
                // Invalidate cache
                _cacheService?.Remove("quiz_questions_all");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question with ID: {QuestionId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting question with ID: {QuestionId}", id);
                
                var question = await _context.QuizQuestions
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == id);
                
                if (question == null)
                {
                    _logger.LogWarning("Question with ID {QuestionId} not found", id);
                    return false;
                }

                _context.QuizQuestions.Remove(question);
                await _context.SaveChangesAsync();
                
                // Invalidate cache
                _cacheService?.Remove("quiz_questions_all");
                _cacheService?.Clear(); // Clear leaderboard cache too
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question with ID: {QuestionId}", id);
                return false;
            }
        }

        public async Task<QuizResult> SubmitQuizAsync(List<QuizAnswer> answers, string userId, TimeSpan duration)
        {
            try
            {
                if (!await ValidateQuizSubmissionAsync(answers))
                {
                    _logger.LogWarning("Quiz submission validation failed for user {UserId}", userId);
                    throw new ValidationException("Invalid quiz submission");
                }

                _logger.LogInformation("Submitting quiz for user {UserId}", userId);

                // Get user name from database
                var user = await _context.Users.FindAsync(userId);
                var userName = user?.Email ?? "Anonymous";

                var questions = await _context.QuizQuestions
                    .Include(q => q.Options)
                    .ToListAsync();
                var correctAnswers = 0;

                foreach (var answer in answers)
                {
                    var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question != null && answer.SelectedOptionId == question.CorrectOptionId)
                    {
                        correctAnswers++;
                    }
                }

                var result = new QuizResult
                {
                    UserId = userId,
                    UserName = userName,
                    TotalQuestions = answers.Count,
                    CorrectAnswers = correctAnswers,
                    CompletedAt = DateTime.UtcNow,
                    Duration = duration,
                    Answers = answers
                };

                _context.QuizResults.Add(result);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cacheService?.Clear();

                _logger.LogInformation("Quiz submitted successfully for user {UserId} with score {Score}%", 
                    userId, result.ScorePercentage);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz for user {UserId}", userId);
                throw;
            }
        }
    
        public async Task<QuizResult?> GetQuizResultByIdAsync(int resultId)
        {
            try
            {
                _logger.LogInformation("Retrieving quiz result with ID: {ResultId}", resultId);
                return await _context.QuizResults
                    .Include(r => r.Answers)
                    .FirstOrDefaultAsync(r => r.Id == resultId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quiz result with ID: {ResultId}", resultId);
                throw;
            }
        }

        public async Task<List<QuizResult>> GetUserQuizHistoryAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Retrieving quiz history for user {UserId}", userId);
                return await _context.QuizResults
                    .Where(r => r.UserId == userId)
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

        public async Task<List<QuizResult>> GetLeaderboardAsync(int topCount = 10)
        {
            var cacheKey = $"leaderboard_top_{topCount}";
            
            if (_cacheService != null)
            {
                return await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await FetchLeaderboardFromDatabaseAsync(topCount);
                }, TimeSpan.FromMinutes(5)) ?? new List<QuizResult>();
            }

            return await FetchLeaderboardFromDatabaseAsync(topCount);
        }

        private async Task<List<QuizResult>> FetchLeaderboardFromDatabaseAsync(int topCount)
        {
            try
            {
                _logger.LogInformation("Retrieving leaderboard with top {TopCount} results", topCount);
                var results = await _context.QuizResults
                    .OrderByDescending(r => (double)r.CorrectAnswers / r.TotalQuestions)
                    .ThenBy(r => r.CompletedAt)
                    .Take(topCount)
                    .ToListAsync();
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leaderboard");
                throw;
            }
        }

        public async Task<bool> ValidateQuizSubmissionAsync(List<QuizAnswer> answers)
        {
            try
            {
                if (answers == null || !answers.Any())
                {
                    _logger.LogWarning("Quiz submission validation failed: No answers provided");
                    return false;
                }

                var questions = await _context.QuizQuestions
                    .Include(q => q.Options)
                    .ToListAsync();
                var questionIds = questions.Select(q => q.Id).ToHashSet();

                foreach (var answer in answers)
                {
                    if (!questionIds.Contains(answer.QuestionId))
                    {
                        _logger.LogWarning("Quiz submission validation failed: Invalid question ID {QuestionId}", answer.QuestionId);
                        return false;
                    }

                    // Allow -1 for unanswered questions
                    if (answer.SelectedOptionId == -1)
                    {
                        continue;
                    }

                    var question = questions.First(q => q.Id == answer.QuestionId);
                    var validOptionIds = question.Options.Select(o => o.Id).ToHashSet();

                    if (!validOptionIds.Contains(answer.SelectedOptionId))
                    {
                        _logger.LogWarning("Quiz submission validation failed: Invalid option ID {OptionId} for question {QuestionId}", 
                            answer.SelectedOptionId, answer.QuestionId);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating quiz submission");
                return false;
            }
        }

        private async Task<bool> ValidateQuestionFormAsync(NewQuestionForm questionForm)
        {
            try
            {
                // Validate question text
                if (_validationService != null)
                {
                    if (!_validationService.IsValidQuestionText(questionForm.QuestionText))
                    {
                        _logger.LogWarning("Question validation failed: Invalid question text");
                        return false;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(questionForm.QuestionText) || questionForm.QuestionText.Length < 10)
                    {
                        _logger.LogWarning("Question validation failed: Empty or too short question text");
                        return false;
                    }
                }

                // Validate category
                if (_validationService != null && !_validationService.IsValidCategory(questionForm.Category))
                {
                    _logger.LogWarning("Question validation failed: Invalid category");
                    return false;
                }

                // Validate difficulty
                if (_validationService != null && !_validationService.IsValidDifficulty(questionForm.Difficulty))
                {
                    _logger.LogWarning("Question validation failed: Invalid difficulty");
                    return false;
                }

                // Validate options
                if (questionForm.OptionTexts == null || questionForm.OptionTexts.Count < 2)
                {
                    _logger.LogWarning("Question validation failed: Insufficient options");
                    return false;
                }

                // Validate each option
                foreach (var optionText in questionForm.OptionTexts)
                {
                    if (_validationService != null)
                    {
                        if (!_validationService.IsValidOptionText(optionText))
                        {
                            _logger.LogWarning("Question validation failed: Invalid option text");
                            return false;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(optionText))
                        {
                            _logger.LogWarning("Question validation failed: Empty option text");
                            return false;
                        }
                    }
                }

                // Validate correct option index
                if (questionForm.CorrectOptionIndex < 0 || questionForm.CorrectOptionIndex >= questionForm.OptionTexts.Count)
                {
                    _logger.LogWarning("Question validation failed: Invalid correct option index");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating question form");
                return false;
            }
        }
    }
} 