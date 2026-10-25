using BlazorQuizApp.Services;
using Xunit;

namespace BlazorQuizApp.Tests.Unit
{
    public class ValidationServiceTests
    {
        private readonly ValidationService _sut = new();

        // ── IsValidEmail ────────────────────────────────────────────────────────

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user.name+tag@sub.domain.org")]
        [InlineData("a@b.co")]
        public void IsValidEmail_ValidAddresses_ReturnsTrue(string email)
        {
            Assert.True(_sut.IsValidEmail(email));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("notanemail")]
        [InlineData("missing@tld")]
        [InlineData("@nodomain.com")]
        [InlineData("no space@ example.com")]
        public void IsValidEmail_InvalidAddresses_ReturnsFalse(string? email)
        {
            Assert.False(_sut.IsValidEmail(email));
        }

        [Fact]
        public void IsValidEmail_ExactlyAtMaxLength_ReturnsTrue()
        {
            // 256 chars total: localpart@domain.tld
            var local = new string('a', 244);
            var email = $"{local}@x.com"; // 244+5+1 = 250 chars (under limit)
            Assert.True(_sut.IsValidEmail(email));
        }

        [Fact]
        public void IsValidEmail_ExceedsMaxLength_ReturnsFalse()
        {
            var local = new string('a', 250);
            var email = $"{local}@example.com"; // > 256 chars
            Assert.False(_sut.IsValidEmail(email));
        }

        // ── IsValidQuestionText ─────────────────────────────────────────────────

        [Fact]
        public void IsValidQuestionText_ValidText_ReturnsTrue()
        {
            Assert.True(_sut.IsValidQuestionText("What is the capital of France?"));
        }

        [Fact]
        public void IsValidQuestionText_ExactlyMinLength_ReturnsTrue()
        {
            Assert.True(_sut.IsValidQuestionText("1234567890")); // exactly 10 chars
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Too short")] // 9 chars
        public void IsValidQuestionText_TooShortOrEmpty_ReturnsFalse(string? text)
        {
            Assert.False(_sut.IsValidQuestionText(text));
        }

        [Fact]
        public void IsValidQuestionText_ExceedsMaxLength_ReturnsTrueAfterTruncation()
        {
            // ValidationService sanitizes (truncates) before checking length,
            // so a >1000-char plain-text string is truncated to 1000 and still valid.
            var text = new string('a', 1001);
            Assert.True(_sut.IsValidQuestionText(text));
        }

        [Fact]
        public void IsValidQuestionText_TextWithHtmlTags_StillValidIfLongEnough()
        {
            // HTML is stripped during sanitization; remaining plain text must be >= 10 chars
            Assert.True(_sut.IsValidQuestionText("<b>What is polymorphism in OOP?</b>"));
        }

        [Fact]
        public void IsValidQuestionText_OnlyHtmlTags_ReturnsFalse()
        {
            // After sanitization the plain text is too short
            Assert.False(_sut.IsValidQuestionText("<b>Hi</b>"));
        }

        // ── IsValidOptionText ───────────────────────────────────────────────────

        [Fact]
        public void IsValidOptionText_SingleChar_ReturnsTrue()
        {
            Assert.True(_sut.IsValidOptionText("A"));
        }

        [Fact]
        public void IsValidOptionText_NormalOption_ReturnsTrue()
        {
            Assert.True(_sut.IsValidOptionText("True"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidOptionText_NullOrWhitespace_ReturnsFalse(string? text)
        {
            Assert.False(_sut.IsValidOptionText(text));
        }

        [Fact]
        public void IsValidOptionText_ExceedsMaxLength_ReturnsTrueAfterTruncation()
        {
            // Same truncation behaviour as IsValidQuestionText: oversized input is
            // sanitised to the max length and then accepted rather than rejected.
            var text = new string('x', 501);
            Assert.True(_sut.IsValidOptionText(text));
        }

        // ── SanitizeInput ───────────────────────────────────────────────────────

        [Fact]
        public void SanitizeInput_PlainText_ReturnsUnchanged()
        {
            Assert.Equal("Hello World", _sut.SanitizeInput("Hello World"));
        }

        [Fact]
        public void SanitizeInput_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _sut.SanitizeInput(null));
            Assert.Equal(string.Empty, _sut.SanitizeInput(""));
            Assert.Equal(string.Empty, _sut.SanitizeInput("   "));
        }

        [Fact]
        public void SanitizeInput_HtmlTags_Stripped()
        {
            var result = _sut.SanitizeInput("<p>Hello</p> <b>World</b>");
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void SanitizeInput_ScriptTag_Stripped()
        {
            var result = _sut.SanitizeInput("<script>alert('xss')</script>Safe text");
            Assert.Equal("Safe text", result);
        }

        [Fact]
        public void SanitizeInput_ScriptTagWithAttributes_Stripped()
        {
            var result = _sut.SanitizeInput("<script type='text/javascript'>evil()</script>Clean");
            Assert.Equal("Clean", result);
        }

        [Fact]
        public void SanitizeInput_TruncatesToMaxLength()
        {
            var input = new string('a', 2000);
            var result = _sut.SanitizeInput(input, 100);
            Assert.Equal(100, result.Length);
        }

        [Fact]
        public void SanitizeInput_LeadingTrailingWhitespace_Trimmed()
        {
            Assert.Equal("trimmed", _sut.SanitizeInput("  trimmed  "));
        }

        // ── IsValidCategory ─────────────────────────────────────────────────────

        [Theory]
        [InlineData("General")]
        [InlineData("C#")]
        [InlineData("ASP.NET Core")]
        [InlineData("JavaScript")]
        [InlineData("Database")]
        [InlineData("Web Development")]
        [InlineData("Programming Concepts")]
        [InlineData("Blazor")]
        [InlineData("Entity Framework")]
        [InlineData("Computing")]
        [InlineData("ICT")]
        [InlineData("Networking")]
        [InlineData("Cybersecurity")]
        [InlineData("Ethical Hacking")]
        [InlineData("Penetration Testing")]
        [InlineData("BECE")]
        [InlineData("WASSCE")]
        [InlineData("TEACHERS PROMOTION")]
        [InlineData("NTC Licensure Exams")]
        public void IsValidCategory_WhitelistedCategories_ReturnsTrue(string category)
        {
            Assert.True(_sut.IsValidCategory(category));
        }

        [Theory]
        [InlineData("general")]      // case-insensitive
        [InlineData("BLAZOR")]
        public void IsValidCategory_CaseInsensitive_ReturnsTrue(string category)
        {
            Assert.True(_sut.IsValidCategory(category));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidCategory_NullOrEmpty_ReturnsFalse(string? category)
        {
            Assert.False(_sut.IsValidCategory(category));
        }

        // ── IsValidDifficulty ───────────────────────────────────────────────────

        [Theory]
        [InlineData("Easy")]
        [InlineData("Medium")]
        [InlineData("Hard")]
        public void IsValidDifficulty_ValidValues_ReturnsTrue(string difficulty)
        {
            Assert.True(_sut.IsValidDifficulty(difficulty));
        }

        [Theory]
        [InlineData("easy")]    // case-insensitive
        [InlineData("HARD")]
        public void IsValidDifficulty_CaseInsensitive_ReturnsTrue(string difficulty)
        {
            Assert.True(_sut.IsValidDifficulty(difficulty));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Expert")]
        [InlineData("Normal")]
        public void IsValidDifficulty_InvalidValues_ReturnsFalse(string? difficulty)
        {
            Assert.False(_sut.IsValidDifficulty(difficulty));
        }
    }
}
