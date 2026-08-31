using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using BlazorQuizApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlazorQuizApp.Tests.Integration
{
    /// <summary>
    /// Integration tests for QuizBusinessService using an isolated EF Core in-memory database.
    /// Each test method gets its own database instance to prevent interference.
    /// </summary>
    public class QuizBusinessServiceTests
    {
        // ── Helpers ─────────────────────────────────────────────────────────────

        private QuizBusinessService CreateService(
            ApplicationDbContext context,
            ICacheService? cache = null,
            IValidationService? validation = null)
        {
            return new QuizBusinessService(
                context,
                NullLogger<QuizBusinessService>.Instance,
                cache,
                validation ?? new ValidationService());
        }

        private static (QuizQuestion question, ApplicationUser user) SeedQuestionAndUser(
            ApplicationDbContext ctx,
            string category = "C#",
            string difficulty = "Medium")
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "test@example.com",
                Email = "test@example.com",
                FullName = "Test User"
            };
            ctx.Users.Add(user);

            var question = new QuizQuestion
            {
                QuestionText = "What does the 'var' keyword do in C#?",
                Category = category,
                Difficulty = difficulty,
                CorrectOptionId = 0,
                Options =
                [
                    new QuizOption { Text = "Declares a variable" },
                    new QuizOption { Text = "Declares a method" },
                    new QuizOption { Text = "Imports a namespace" },
                    new QuizOption { Text = "Creates a class" }
                ]
            };
            ctx.QuizQuestions.Add(question);
            ctx.SaveChanges();

            // Set correct option after IDs are assigned
            question.CorrectOptionId = question.Options[0].Id;
            ctx.SaveChanges();

            return (question, user);
        }

        // ── GetQuestionsAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetQuestionsAsync_EmptyDb_ReturnsEmptyList()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);

            var result = await svc.GetQuestionsAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetQuestionsAsync_WithData_ReturnsAllQuestionsWithOptions()
        {
            using var ctx = DbContextHelper.Create();
            SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var result = await svc.GetQuestionsAsync();

            Assert.Single(result);
            Assert.Equal(4, result[0].Options.Count);
        }

        // ── GetQuestionByIdAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetQuestionByIdAsync_ExistingId_ReturnsQuestion()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var result = await svc.GetQuestionByIdAsync(question.Id);

            Assert.NotNull(result);
            Assert.Equal(question.Id, result!.Id);
            Assert.Equal(4, result.Options.Count);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_NonExistentId_ReturnsNull()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);

            var result = await svc.GetQuestionByIdAsync(99999);

            Assert.Null(result);
        }

        // ── AddQuestionAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task AddQuestionAsync_ValidForm_PersistsToDb()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "What is dependency injection in software engineering?",
                Category = "General",
                Difficulty = "Medium",
                OptionTexts = ["Design pattern", "Anti-pattern", "A library", "A framework"],
                CorrectOptionIndex = 0
            };

            var success = await svc.AddQuestionAsync(form);

            Assert.True(success);
            Assert.Single(ctx.QuizQuestions);
            var saved = ctx.QuizQuestions.First();
            Assert.Equal(4, ctx.QuizOptions.Count());
            Assert.NotEqual(0, saved.CorrectOptionId);
        }

        [Fact]
        public async Task AddQuestionAsync_CorrectOptionIdIsSetToSelectedOption()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "What is the output of 1 + 1 in C sharp?",
                Category = "C#",
                Difficulty = "Easy",
                OptionTexts = ["1", "2", "3", "4"],
                CorrectOptionIndex = 1 // "2" is correct
            };

            await svc.AddQuestionAsync(form);

            var saved = ctx.QuizQuestions.Include(q => q.Options).First();
            var expectedOption = saved.Options.ElementAt(1);
            Assert.Equal(expectedOption.Id, saved.CorrectOptionId);
        }

        [Fact]
        public async Task AddQuestionAsync_QuestionTooShort_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "Short?",
                Category = "General",
                Difficulty = "Easy",
                OptionTexts = ["Yes", "No", "Maybe", "Never"],
                CorrectOptionIndex = 0
            };

            var success = await svc.AddQuestionAsync(form);

            Assert.False(success);
            Assert.Empty(ctx.QuizQuestions);
        }

        [Fact]
        public async Task AddQuestionAsync_EmptyOptionText_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "What is a valid question for testing purposes?",
                Category = "General",
                Difficulty = "Easy",
                OptionTexts = ["Option A", "", "Option C", "Option D"],
                CorrectOptionIndex = 0
            };

            var success = await svc.AddQuestionAsync(form);

            Assert.False(success);
        }

        [Fact]
        public async Task AddQuestionAsync_TooFewOptions_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "What is a valid question for testing purposes?",
                Category = "General",
                Difficulty = "Easy",
                OptionTexts = ["Only one"],
                CorrectOptionIndex = 0
            };

            var success = await svc.AddQuestionAsync(form);

            Assert.False(success);
        }

        [Fact]
        public async Task AddQuestionAsync_InvalidDifficulty_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "What is a valid question for testing purposes?",
                Category = "General",
                Difficulty = "Expert",
                OptionTexts = ["A", "B", "C", "D"],
                CorrectOptionIndex = 0
            };

            var success = await svc.AddQuestionAsync(form);

            Assert.False(success);
        }

        [Fact]
        public async Task AddQuestionAsync_SanitizesHtmlFromInputs()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "<b>What does HTML stand for in web development context?</b>",
                Category = "Web Development",
                Difficulty = "Easy",
                OptionTexts = ["<i>HyperText</i>", "HighText", "HyperType", "None"],
                CorrectOptionIndex = 0
            };

            await svc.AddQuestionAsync(form);

            var saved = ctx.QuizQuestions.Include(q => q.Options).First();
            Assert.DoesNotContain("<b>", saved.QuestionText);
            Assert.DoesNotContain("<i>", saved.Options.First().Text);
        }

        // ── UpdateQuestionAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task UpdateQuestionAsync_ValidUpdate_PersistsChanges()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "Updated question text with enough chars here",
                Category = "Blazor",
                Difficulty = "Hard",
                OptionTexts = ["New A", "New B", "New C", "New D"],
                CorrectOptionIndex = 2
            };

            var success = await svc.UpdateQuestionAsync(question.Id, form);

            Assert.True(success);
            var updated = ctx.QuizQuestions.Include(q => q.Options).First();
            Assert.Equal("Updated question text with enough chars here", updated.QuestionText);
            Assert.Equal("Blazor", updated.Category);
            Assert.Equal("Hard", updated.Difficulty);
            Assert.Equal(4, updated.Options.Count);
        }

        [Fact]
        public async Task UpdateQuestionAsync_ReplacesAllOptions()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "What is the purpose of async await pattern here?",
                Category = "C#",
                Difficulty = "Medium",
                OptionTexts = ["Only two options", "Second option"],
                CorrectOptionIndex = 0
            };

            await svc.UpdateQuestionAsync(question.Id, form);

            // Original had 4 options; after update only 2 remain
            Assert.Equal(2, ctx.QuizOptions.Count());
        }

        [Fact]
        public async Task UpdateQuestionAsync_NonExistentId_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);
            var form = new NewQuestionForm
            {
                QuestionText = "Does not matter since id is invalid here",
                Category = "General",
                Difficulty = "Easy",
                OptionTexts = ["A", "B", "C", "D"],
                CorrectOptionIndex = 0
            };

            var success = await svc.UpdateQuestionAsync(99999, form);

            Assert.False(success);
        }

        // ── DeleteQuestionAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task DeleteQuestionAsync_ExistingId_RemovesFromDb()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var success = await svc.DeleteQuestionAsync(question.Id);

            Assert.True(success);
            Assert.Empty(ctx.QuizQuestions);
        }

        [Fact]
        public async Task DeleteQuestionAsync_CascadesOptions()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            await svc.DeleteQuestionAsync(question.Id);

            Assert.Empty(ctx.QuizOptions);
        }

        [Fact]
        public async Task DeleteQuestionAsync_NonExistentId_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var svc = CreateService(ctx);

            var success = await svc.DeleteQuestionAsync(99999);

            Assert.False(success);
        }

        // ── SubmitQuizAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitQuizAsync_ScoredMode_PersistsResult()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = question.CorrectOptionId }
            };

            var result = await svc.SubmitQuizAsync(
                answers, user.Id, TimeSpan.FromMinutes(2), "C#", false, 10);

            Assert.Equal(1, ctx.QuizResults.Count());
            Assert.Equal(1, result.CorrectAnswers);
            Assert.Equal(1, result.TotalQuestions);
            Assert.Equal(1, result.Score); // ignores the forged client score of 10
        }

        [Fact]
        public async Task GetQuizResultByIdAsync_OtherUser_ReturnsNull()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var result = await svc.SubmitQuizAsync(
                [new QuizAnswer { QuestionId = question.Id, SelectedOptionId = question.CorrectOptionId }],
                user.Id, TimeSpan.FromSeconds(10), "C#", false, 999);

            Assert.Null(await svc.GetQuizResultByIdAsync(result.Id, "another-user"));
            Assert.NotNull(await svc.GetQuizResultByIdAsync(result.Id, user.Id));
        }

        [Fact]
        public async Task SubmitQuizAsync_PracticeMode_DoesNotPersistResult()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = question.CorrectOptionId }
            };

            await svc.SubmitQuizAsync(
                answers, user.Id, TimeSpan.FromMinutes(1), "C#", true, 10);

            Assert.Empty(ctx.QuizResults);
        }

        [Fact]
        public async Task SubmitQuizAsync_WrongAnswer_CountsZeroCorrect()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var wrongOptionId = question.Options.Last().Id;
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = wrongOptionId }
            };

            var result = await svc.SubmitQuizAsync(
                answers, user.Id, TimeSpan.FromMinutes(1), "C#", false, 0);

            Assert.Equal(0, result.CorrectAnswers);
        }

        [Fact]
        public async Task SubmitQuizAsync_UnknownUserId_SetsAnonymousUserName()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = -1 }
            };

            var result = await svc.SubmitQuizAsync(
                answers, "nonexistent-user-id", TimeSpan.FromMinutes(1), "C#", false, 0);

            Assert.Equal("Anonymous", result.UserName);
        }

        [Fact]
        public async Task SubmitQuizAsync_EmptyAnswers_ThrowsValidationException()
        {
            using var ctx = DbContextHelper.Create();
            SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
                () => svc.SubmitQuizAsync([], "user1", TimeSpan.Zero, "C#", false, 0));
        }

        // ── GetUserQuizHistoryAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetUserQuizHistoryAsync_ReturnsOnlyScoredResults()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = -1 }
            };

            await svc.SubmitQuizAsync(answers, user.Id, TimeSpan.FromMinutes(1), "C#", false, 5); // scored
            await svc.SubmitQuizAsync(answers, user.Id, TimeSpan.FromMinutes(1), "C#", true, 5);  // practice

            var history = await svc.GetUserQuizHistoryAsync(user.Id);

            Assert.Single(history);
            Assert.False(history[0].IsPracticeMode);
        }

        [Fact]
        public async Task GetUserQuizHistoryAsync_ReturnsOnlyCurrentUsersResults()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = -1 }
            };

            await svc.SubmitQuizAsync(answers, user.Id, TimeSpan.FromMinutes(1), "C#", false, 5);
            await svc.SubmitQuizAsync(answers, "other-user-id", TimeSpan.FromMinutes(1), "C#", false, 8);

            var history = await svc.GetUserQuizHistoryAsync(user.Id);

            Assert.Single(history);
            Assert.Equal(user.Id, history[0].UserId);
        }

        [Fact]
        public async Task GetUserQuizHistoryAsync_OrderedByMostRecentFirst()
        {
            using var ctx = DbContextHelper.Create();
            var (question, user) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = -1 }
            };

            // Seed two results with different timestamps
            ctx.QuizResults.Add(new QuizResult
            {
                UserId = user.Id, UserName = "test@example.com",
                Category = "C#", TotalQuestions = 1, CorrectAnswers = 0,
                Score = 0, IsPracticeMode = false,
                CompletedAt = DateTime.UtcNow.AddMinutes(-10)
            });
            ctx.QuizResults.Add(new QuizResult
            {
                UserId = user.Id, UserName = "test@example.com",
                Category = "C#", TotalQuestions = 1, CorrectAnswers = 1,
                Score = 5, IsPracticeMode = false,
                CompletedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var history = await svc.GetUserQuizHistoryAsync(user.Id);

            Assert.Equal(2, history.Count);
            Assert.True(history[0].CompletedAt >= history[1].CompletedAt);
        }

        // ── GetLeaderboardAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetLeaderboardAsync_ExcludesPracticeResults()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            ctx.QuizResults.Add(new QuizResult
            {
                UserId = user.Id, UserName = "u", Category = "C#",
                TotalQuestions = 5, CorrectAnswers = 5, Score = 10,
                IsPracticeMode = true
            });
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var board = await svc.GetLeaderboardAsync();

            Assert.Empty(board);
        }

        [Fact]
        public async Task GetLeaderboardAsync_OrderedByScoreDescending()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            ctx.QuizResults.AddRange(
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 5, CorrectAnswers = 3, Score = 6, IsPracticeMode = false, Duration = TimeSpan.FromMinutes(5) },
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 5, CorrectAnswers = 5, Score = 10, IsPracticeMode = false, Duration = TimeSpan.FromMinutes(3) },
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 5, CorrectAnswers = 1, Score = 2, IsPracticeMode = false, Duration = TimeSpan.FromMinutes(7) }
            );
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var board = await svc.GetLeaderboardAsync();

            Assert.Equal(10, board[0].Score);
            Assert.Equal(6, board[1].Score);
            Assert.Equal(2, board[2].Score);
        }

        [Fact]
        public async Task GetLeaderboardAsync_TopCountRespected()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            for (int i = 1; i <= 15; i++)
            {
                ctx.QuizResults.Add(new QuizResult
                {
                    UserId = user.Id, UserName = "u", Category = "C#",
                    TotalQuestions = 5, CorrectAnswers = i % 5, Score = i,
                    IsPracticeMode = false, Duration = TimeSpan.FromMinutes(5)
                });
            }
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var board = await svc.GetLeaderboardAsync(topCount: 5);

            Assert.Equal(5, board.Count);
        }

        [Fact]
        public async Task GetLeaderboardAsync_CategoryFilter_OnlyReturnsThatCategory()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            ctx.QuizResults.AddRange(
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 5, CorrectAnswers = 5, Score = 10, IsPracticeMode = false, Duration = TimeSpan.FromMinutes(2) },
                new QuizResult { UserId = user.Id, UserName = "u", Category = "JavaScript", TotalQuestions = 5, CorrectAnswers = 4, Score = 8, IsPracticeMode = false, Duration = TimeSpan.FromMinutes(3) }
            );
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var board = await svc.GetLeaderboardAsync(category: "C#");

            Assert.Single(board);
            Assert.Equal("C#", board[0].Category);
        }

        // ── GetCategoryStatsAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetCategoryStatsAsync_ReturnsCorrectAggregations()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            ctx.QuizResults.AddRange(
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 10, CorrectAnswers = 8, Score = 16, IsPracticeMode = false },
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 10, CorrectAnswers = 6, Score = 12, IsPracticeMode = false },
                new QuizResult { UserId = user.Id, UserName = "u", Category = "Blazor", TotalQuestions = 10, CorrectAnswers = 9, Score = 18, IsPracticeMode = false }
            );
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var stats = await svc.GetCategoryStatsAsync(user.Id);

            Assert.Equal(2, stats.Count);
            var csharp = stats.First(s => s.Category == "C#");
            Assert.Equal(2, csharp.QuizzesTaken);
            Assert.Equal(80.0, csharp.BestScore, 1);
            Assert.Equal(70.0, csharp.AverageScore, 1);
            Assert.Equal(28, csharp.TotalPoints);
        }

        [Fact]
        public async Task GetCategoryStatsAsync_ExcludesPracticeResults()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            ctx.QuizResults.Add(new QuizResult
            {
                UserId = user.Id, UserName = "u", Category = "C#",
                TotalQuestions = 5, CorrectAnswers = 5, Score = 10,
                IsPracticeMode = true
            });
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var stats = await svc.GetCategoryStatsAsync(user.Id);

            Assert.Empty(stats);
        }

        // ── GetBestScoresPerCategoryAsync ───────────────────────────────────────

        [Fact]
        public async Task GetBestScoresPerCategoryAsync_ReturnsBestPerCategory()
        {
            using var ctx = DbContextHelper.Create();
            var (_, user) = SeedQuestionAndUser(ctx);
            ctx.QuizResults.AddRange(
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 10, CorrectAnswers = 7, Score = 14, IsPracticeMode = false },
                new QuizResult { UserId = user.Id, UserName = "u", Category = "C#", TotalQuestions = 10, CorrectAnswers = 9, Score = 18, IsPracticeMode = false }
            );
            ctx.SaveChanges();
            var svc = CreateService(ctx);

            var best = await svc.GetBestScoresPerCategoryAsync(user.Id);

            Assert.True(best.ContainsKey("C#"));
            Assert.Equal(90.0, best["C#"], 1);
        }

        // ── ValidateQuizSubmissionAsync ─────────────────────────────────────────

        [Fact]
        public async Task ValidateQuizSubmissionAsync_NullAnswers_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var valid = await svc.ValidateQuizSubmissionAsync(null!);

            Assert.False(valid);
        }

        [Fact]
        public async Task ValidateQuizSubmissionAsync_EmptyAnswers_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);

            var valid = await svc.ValidateQuizSubmissionAsync([]);

            Assert.False(valid);
        }

        [Fact]
        public async Task ValidateQuizSubmissionAsync_ValidAnswerWithSkipped_ReturnsTrue()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            // SelectedOptionId == -1 means the question was skipped (allowed)
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = -1 }
            };

            var valid = await svc.ValidateQuizSubmissionAsync(answers);

            Assert.True(valid);
        }

        [Fact]
        public async Task ValidateQuizSubmissionAsync_AnswerWithValidOption_ReturnsTrue()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = question.CorrectOptionId }
            };

            var valid = await svc.ValidateQuizSubmissionAsync(answers);

            Assert.True(valid);
        }

        [Fact]
        public async Task ValidateQuizSubmissionAsync_QuestionNotInDb_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = 99999, SelectedOptionId = 1 }
            };

            var valid = await svc.ValidateQuizSubmissionAsync(answers);

            Assert.False(valid);
        }

        [Fact]
        public async Task ValidateQuizSubmissionAsync_OptionNotBelongingToQuestion_ReturnsFalse()
        {
            using var ctx = DbContextHelper.Create();
            var (question, _) = SeedQuestionAndUser(ctx);
            var svc = CreateService(ctx);
            const int bogusOptionId = 99999;
            var answers = new List<QuizAnswer>
            {
                new() { QuestionId = question.Id, SelectedOptionId = bogusOptionId }
            };

            var valid = await svc.ValidateQuizSubmissionAsync(answers);

            Assert.False(valid);
        }
    }
}
