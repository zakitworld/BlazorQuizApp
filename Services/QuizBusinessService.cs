using BlazorQuizApp.Data;
using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Services
{
    public class QuizBusinessService : IQuizBusinessService
    {
        private readonly ApplicationDbContext _context;
        private readonly QuizService _quizService;
        private readonly ILogger<QuizBusinessService> _logger;

        public QuizBusinessService(ApplicationDbContext context, QuizService quizService, ILogger<QuizBusinessService> logger)
        {
            _context = context;
            _quizService = quizService;
            _logger = logger;
        }

        public async Task<List<QuizQuestion>> GetQuestionsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving quiz questions");
                return await Task.FromResult(_quizService.Questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quiz questions");
                throw;
            }
        }

        public async Task<QuizQuestion?> GetQuestionByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving question with ID: {QuestionId}", id);
                return await Task.FromResult(_quizService.GetQuestionById(id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving question with ID: {QuestionId}", id);
                throw;
            }
        }

        public async Task<bool> AddQuestionAsync(NewQuestionForm questionForm)
        {
            try
            {
                if (!await ValidateQuestionFormAsync(questionForm))
                {
                    return false;
                }

                _logger.LogInformation("Adding new question: {QuestionText}", questionForm.QuestionText);
                _quizService.AddQuestion(questionForm);
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
                _quizService.UpdateQuestion(id, questionForm);
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
                _quizService.DeleteQuestion(id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question with ID: {QuestionId}", id);
                return false;
            }
        }

        public async Task<QuizResult> SubmitQuizAsync(List<QuizAnswer> answers, string userId)
        {
            try
            {
                if (!await ValidateQuizSubmissionAsync(answers))
                {
                    throw new ValidationException("Invalid quiz submission");
                }

                var questions = await GetQuestionsAsync();
                var correctAnswers = 0;

                foreach (var answer in answers)
                {
                    var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question != null && answer.SelectedOptionId == question.CorrectOptionId)
                    {
                        correctAnswers++;
                    }
                }

                // Get user name from database
                var user = await _context.Users.FindAsync(userId);
                var userName = user?.Email ?? "Anonymous";

                var result = new QuizResult
                {
                    UserId = userId,
                    UserName = userName,
                    TotalQuestions = answers.Count,
                    CorrectAnswers = correctAnswers,
                    Answers = answers,
                    CompletedAt = DateTime.UtcNow
                };

                _context.QuizResults.Add(result);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Quiz submitted for user {UserId} with score {Score}%", 
                    userId, result.ScorePercentage);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz for user {UserId}", userId);
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
            try
            {
                _logger.LogInformation("Retrieving leaderboard with top {TopCount} results", topCount);
                return await _context.QuizResults
                    .OrderByDescending(r => r.ScorePercentage)
                    .ThenBy(r => r.CompletedAt)
                    .Take(topCount)
                    .ToListAsync();
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

                var questions = await GetQuestionsAsync();
                var questionIds = questions.Select(q => q.Id).ToHashSet();

                foreach (var answer in answers)
                {
                    if (!questionIds.Contains(answer.QuestionId))
                    {
                        _logger.LogWarning("Quiz submission validation failed: Invalid question ID {QuestionId}", answer.QuestionId);
                        return false;
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
                if (string.IsNullOrWhiteSpace(questionForm.QuestionText))
                {
                    _logger.LogWarning("Question validation failed: Empty question text");
                    return false;
                }

                if (questionForm.OptionTexts == null || questionForm.OptionTexts.Count < 2)
                {
                    _logger.LogWarning("Question validation failed: Insufficient options");
                    return false;
                }

                if (questionForm.OptionTexts.Any(string.IsNullOrWhiteSpace))
                {
                    _logger.LogWarning("Question validation failed: Empty option text");
                    return false;
                }

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