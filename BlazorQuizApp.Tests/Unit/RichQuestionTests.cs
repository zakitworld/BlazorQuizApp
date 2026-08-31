using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using System.Text.Json;
using Xunit;

namespace BlazorQuizApp.Tests.Unit;

public class RichQuestionTests
{
    [Fact]
    public void MultipleSelect_RequiresExactSet()
    {
        var question = new QuizQuestion { Type = QuestionType.MultipleSelect, CorrectOptionIdsJson = "[2,4]" };
        Assert.True(QuizBusinessService.IsAnswerCorrect(question,
            new QuizAnswer { SelectedOptionIdsJson = JsonSerializer.Serialize(new[] { 4, 2 }) }));
        Assert.False(QuizBusinessService.IsAnswerCorrect(question,
            new QuizAnswer { SelectedOptionIdsJson = JsonSerializer.Serialize(new[] { 2 }) }));
    }

    [Fact]
    public void ShortAnswer_NormalizesCaseAndPunctuation()
    {
        var question = new QuizQuestion
        {
            Type = QuestionType.ShortAnswer,
            AcceptedAnswersJson = JsonSerializer.Serialize(new[] { "Dependency Injection" })
        };
        Assert.True(QuizBusinessService.IsAnswerCorrect(question,
            new QuizAnswer { TextAnswer = "dependency injection!" }));
    }

    [Fact]
    public void ShortAnswer_RejectsUnlistedAnswer()
    {
        var question = new QuizQuestion
        {
            Type = QuestionType.ShortAnswer,
            AcceptedAnswersJson = JsonSerializer.Serialize(new[] { "Blazor" })
        };
        Assert.False(QuizBusinessService.IsAnswerCorrect(question,
            new QuizAnswer { TextAnswer = "MVC" }));
    }
}
