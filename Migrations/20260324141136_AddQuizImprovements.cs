using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "QuizResults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPracticeMode",
                table: "QuizResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "QuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "QuizQuestions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeTakenSeconds",
                table: "QuizAnswers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "IsPracticeMode",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "TimeTakenSeconds",
                table: "QuizAnswers");
        }
    }
}
