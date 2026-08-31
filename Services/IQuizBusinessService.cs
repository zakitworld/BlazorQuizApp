using BlazorQuizApp.Models;

namespace BlazorQuizApp.Services
{
    public record CategoryStat(
        string Category,
        int QuizzesTaken,
        double BestScore,
        double AverageScore,
        int TotalPoints,
        int BestStreak);

    public interface IQuizBusinessService
    {
        Task<List<QuizQuestion>> GetQuestionsAsync();
        Task<QuizQuestion?> GetQuestionByIdAsync(int id);
        Task<bool> AddQuestionAsync(NewQuestionForm questionForm);
        Task<bool> UpdateQuestionAsync(int id, NewQuestionForm questionForm);
        Task<bool> DeleteQuestionAsync(int id);
        Task<QuizResult> SubmitQuizAsync(List<QuizAnswer> answers, string userId, TimeSpan duration,
            string category, bool isPracticeMode, int score, int? assignmentId = null,
            int attemptNumber = 0, int passPercentage = 0);
        Task<QuizResult?> GetQuizResultByIdAsync(int resultId, string userId);
        Task<List<QuizResult>> GetUserQuizHistoryAsync(string userId);
        Task<List<QuizResult>> GetLeaderboardAsync(int topCount = 10, string? category = null);
        Task<List<CategoryStat>> GetCategoryStatsAsync(string userId);
        Task<Dictionary<string, double>> GetBestScoresPerCategoryAsync(string userId);
        Task<bool> ValidateQuizSubmissionAsync(List<QuizAnswer> answers);
    }
}
