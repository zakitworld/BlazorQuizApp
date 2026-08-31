using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BlazorQuizApp.Services;

public record QuestionQuality(int Score, List<string> Warnings, List<int> SimilarQuestionIds);
public class StudioDraft
{
    public NewQuestionForm Form { get; set; } = new();
    public QuestionQuality Quality { get; set; } = new(100, [], []);
    public bool Approved { get; set; }
}

public interface IQuestionStudioService
{
    Task<List<StudioDraft>> GenerateDraftsAsync(string sourceText, string category, string difficulty, int count);
    Task<QuestionQuality> AnalyzeAsync(NewQuestionForm form);
    Task<int> PublishApprovedAsync(IEnumerable<StudioDraft> drafts);
}

public sealed class QuestionStudioService(ApplicationDbContext db, IQuizBusinessService quizService)
    : IQuestionStudioService
{
    public async Task<List<StudioDraft>> GenerateDraftsAsync(string sourceText, string category, string difficulty, int count)
    {
        var sentences = Regex.Split(sourceText.Trim(), @"(?<=[.!?])\s+")
            .Select(Clean).Where(x => x.Length is >= 25 and <= 350).Distinct().Take(30).ToList();
        if (sentences.Count < 2) throw new ArgumentException("Add at least two clear factual sentences to generate useful drafts.");
        var drafts = new List<StudioDraft>();
        for (var i = 0; i < Math.Clamp(count, 1, 12) && i < sentences.Count; i++)
        {
            var correct = sentences[i];
            var distractors = sentences.Where((_, index) => index != i).OrderBy(_ => Random.Shared.Next()).Take(3).ToList();
            while (distractors.Count < 3) distractors.Add("None of the statements provided describes this concept accurately.");
            var options = new[] { correct }.Concat(distractors).OrderBy(_ => Random.Shared.Next()).ToList();
            var form = new NewQuestionForm
            {
                QuestionText = $"Which statement is accurate according to the learning material?",
                Category = category, Difficulty = difficulty, Type = QuestionType.MultipleChoice,
                OptionTexts = options, CorrectOptionIndex = options.IndexOf(correct),
                Explanation = $"The source material states: {correct}"
            };
            drafts.Add(new StudioDraft { Form = form, Quality = await AnalyzeAsync(form) });
        }
        return drafts;
    }

    public async Task<QuestionQuality> AnalyzeAsync(NewQuestionForm form)
    {
        var warnings = new List<string>();
        if (form.QuestionText.Length < 20) warnings.Add("The prompt may be too short to be unambiguous.");
        if (Regex.IsMatch(form.QuestionText, @"\b(always|never|obviously|all of the above)\b", RegexOptions.IgnoreCase))
            warnings.Add("Avoid absolute or answer-signalling language.");
        if (form.Type != QuestionType.ShortAnswer && form.OptionTexts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != form.OptionTexts.Count)
            warnings.Add("Two or more options are duplicates.");
        if (form.Type == QuestionType.MultipleChoice && form.CorrectOptionIndex >= form.OptionTexts.Count)
            warnings.Add("Choose a valid correct option.");
        if (form.Type == QuestionType.MultipleSelect && form.CorrectOptionIndexes.Count == 0)
            warnings.Add("Select at least one correct answer.");
        if (string.IsNullOrWhiteSpace(form.Explanation)) warnings.Add("Add an explanation to reinforce learning.");

        var terms = Terms(form.QuestionText);
        var existing = await db.QuizQuestions.AsNoTracking().Select(q => new { q.Id, q.QuestionText }).ToListAsync();
        var similar = existing.Where(q => Jaccard(terms, Terms(q.QuestionText)) >= .7).Select(q => q.Id).Take(5).ToList();
        if (similar.Count > 0) warnings.Add("This looks similar to an existing question.");
        return new QuestionQuality(Math.Max(0, 100 - warnings.Count * 18), warnings, similar);
    }

    public async Task<int> PublishApprovedAsync(IEnumerable<StudioDraft> drafts)
    {
        var count = 0;
        foreach (var draft in drafts.Where(d => d.Approved))
            if (await quizService.AddQuestionAsync(draft.Form)) count++;
        return count;
    }

    private static string Clean(string value) => Regex.Replace(value, @"\s+", " ").Trim(' ', '\r', '\n');
    private static HashSet<string> Terms(string value) => Regex.Matches(value.ToLowerInvariant(), @"[a-z0-9]{3,}")
        .Select(x => x.Value).Where(x => !new[] { "the", "and", "that", "with", "from", "this" }.Contains(x)).ToHashSet();
    private static double Jaccard(HashSet<string> a, HashSet<string> b) => a.Count == 0 || b.Count == 0 ? 0 :
        (double)a.Intersect(b).Count() / a.Union(b).Count();
}
