using BlazorQuizApp.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace BlazorQuizApp.Tests.Helpers
{
    /// <summary>
    /// Creates a real QuizService backed by a temp directory (no questions.json),
    /// so it falls back to GetDefaultQuestions(). Safe to use as the fallback
    /// parameter in QuizBusinessService under test.
    /// </summary>
    public static class QuizServiceHelper
    {
        public static QuizService Create()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BlazorQuizTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(tempDir);

            return new QuizService(mockEnv.Object);
        }
    }
}
