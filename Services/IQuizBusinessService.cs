using BlazorQuizApp.Models;

namespace BlazorQuizApp.Services
{
    public interface IQuizBusinessService
    {
        Task<List<QuizQuestion>> GetQuestionsAsync();
        Task<QuizQuestion?> GetQuestionByIdAsync(int id);
        Task<bool> AddQuestionAsync(NewQuestionForm questionForm);
        Task<bool> UpdateQuestionAsync(int id, NewQuestionForm questionForm);
        Task<bool> DeleteQuestionAsync(int id);
        Task<QuizResult> SubmitQuizAsync(List<QuizAnswer> answers, string userId);
        Task<List<QuizResult>> GetUserQuizHistoryAsync(string userId);
        Task<List<QuizResult>> GetLeaderboardAsync(int topCount = 10);
        Task<bool> ValidateQuizSubmissionAsync(List<QuizAnswer> answers);
    }
} 