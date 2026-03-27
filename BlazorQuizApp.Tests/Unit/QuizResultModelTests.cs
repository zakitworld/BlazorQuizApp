using BlazorQuizApp.Models;
using Xunit;

namespace BlazorQuizApp.Tests.Unit
{
    public class QuizResultModelTests
    {
        // ── ScorePercentage computed property ───────────────────────────────────

        [Fact]
        public void ScorePercentage_AllCorrect_Returns100()
        {
            var result = new QuizResult
            {
                TotalQuestions = 10,
                CorrectAnswers = 10
            };
            Assert.Equal(100.0, result.ScorePercentage, precision: 5);
        }

        [Fact]
        public void ScorePercentage_NoneCorrect_Returns0()
        {
            var result = new QuizResult
            {
                TotalQuestions = 10,
                CorrectAnswers = 0
            };
            Assert.Equal(0.0, result.ScorePercentage, precision: 5);
        }

        [Fact]
        public void ScorePercentage_HalfCorrect_Returns50()
        {
            var result = new QuizResult
            {
                TotalQuestions = 10,
                CorrectAnswers = 5
            };
            Assert.Equal(50.0, result.ScorePercentage, precision: 5);
        }

        [Fact]
        public void ScorePercentage_ZeroTotalQuestions_Returns0_NoDivideByZero()
        {
            var result = new QuizResult
            {
                TotalQuestions = 0,
                CorrectAnswers = 0
            };
            Assert.Equal(0.0, result.ScorePercentage, precision: 5);
        }

        [Fact]
        public void ScorePercentage_FractionalResult_CalculatedCorrectly()
        {
            var result = new QuizResult
            {
                TotalQuestions = 3,
                CorrectAnswers = 2
            };
            Assert.Equal(66.666666, result.ScorePercentage, precision: 4);
        }

        // ── Default property values ─────────────────────────────────────────────

        [Fact]
        public void NewQuizResult_AnswersList_DefaultsToEmpty()
        {
            var result = new QuizResult();
            Assert.NotNull(result.Answers);
            Assert.Empty(result.Answers);
        }

        [Fact]
        public void NewQuizResult_CompletedAt_DefaultsToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var result = new QuizResult();
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(result.CompletedAt, before, after);
        }

        // ── QuizQuestion defaults ───────────────────────────────────────────────

        [Fact]
        public void NewQuizQuestion_Options_DefaultsToEmpty()
        {
            var q = new QuizQuestion { QuestionText = "Test?", CorrectOptionId = 0 };
            Assert.NotNull(q.Options);
            Assert.Empty(q.Options);
        }

        [Fact]
        public void NewQuizQuestion_SelectedOptionId_DefaultsToMinus1()
        {
            var q = new QuizQuestion { QuestionText = "Test?", CorrectOptionId = 0 };
            Assert.Equal(-1, q.SelectedOptionId);
        }

        [Fact]
        public void NewQuizQuestion_Category_DefaultsToGeneral()
        {
            var q = new QuizQuestion { QuestionText = "Test?", CorrectOptionId = 0 };
            Assert.Equal("General", q.Category);
        }

        [Fact]
        public void NewQuizQuestion_Difficulty_DefaultsToMedium()
        {
            var q = new QuizQuestion { QuestionText = "Test?", CorrectOptionId = 0 };
            Assert.Equal("Medium", q.Difficulty);
        }

        // ── NewQuestionForm defaults ────────────────────────────────────────────

        [Fact]
        public void NewQuestionForm_OptionTexts_DefaultsToFourSlots()
        {
            var form = new NewQuestionForm();
            Assert.Equal(4, form.OptionTexts.Count);
        }

        [Fact]
        public void NewQuestionForm_CorrectOptionIndex_DefaultsToZero()
        {
            var form = new NewQuestionForm();
            Assert.Equal(0, form.CorrectOptionIndex);
        }

        [Fact]
        public void NewQuestionForm_Category_DefaultsToGeneral()
        {
            var form = new NewQuestionForm();
            Assert.Equal("General", form.Category);
        }

        // ── Subscription defaults ───────────────────────────────────────────────

        [Fact]
        public void NewSubscription_Tier_DefaultsToFree()
        {
            var sub = new Subscription { UserId = "user1" };
            Assert.Equal(SubscriptionTier.Free, sub.Tier);
        }

        [Fact]
        public void NewSubscription_IsActive_DefaultsToTrue()
        {
            var sub = new Subscription { UserId = "user1" };
            Assert.True(sub.IsActive);
        }

        [Fact]
        public void NewSubscription_AutoRenew_DefaultsToTrue()
        {
            var sub = new Subscription { UserId = "user1" };
            Assert.True(sub.AutoRenew);
        }
    }
}
