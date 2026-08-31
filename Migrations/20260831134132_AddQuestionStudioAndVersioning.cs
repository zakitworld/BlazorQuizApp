using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionStudioAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedAnswersJson",
                table: "QuizQuestions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectOptionIdsJson",
                table: "QuizQuestions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "QuizQuestions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "QuizQuestions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswerSnapshot",
                table: "QuizAnswers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "QuizAnswers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QuestionTextSnapshot",
                table: "QuizAnswers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionVersionNumber",
                table: "QuizAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SelectedAnswerSnapshot",
                table: "QuizAnswers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionIdsJson",
                table: "QuizAnswers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextAnswer",
                table: "QuizAnswers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizQuestionId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswersJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionVersions_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionVersions_QuizQuestionId_VersionNumber",
                table: "QuestionVersions",
                columns: new[] { "QuizQuestionId", "VersionNumber" },
                unique: true);

            migrationBuilder.Sql("""
                UPDATE QuizQuestions
                SET CorrectOptionIdsJson = CONCAT('[', CorrectOptionId, ']')
                WHERE CorrectOptionId > 0;

                INSERT INTO QuestionVersions
                    (QuizQuestionId, VersionNumber, QuestionText, OptionsJson, Type,
                     CorrectAnswersJson, Explanation, ChangedByUserId, CreatedAt)
                SELECT q.Id, 1, q.QuestionText,
                       (SELECT o.Id, o.Text FROM QuizOptions o WHERE o.QuizQuestionId = q.Id FOR JSON PATH),
                       q.Type, q.CorrectOptionIdsJson, q.Explanation, NULL, SYSUTCDATETIME()
                FROM QuizQuestions q;

                UPDATE a
                SET a.QuestionTextSnapshot = q.QuestionText,
                    a.SelectedAnswerSnapshot = selectedOption.Text,
                    a.CorrectAnswerSnapshot = correctOption.Text,
                    a.IsCorrect = CASE WHEN a.SelectedOptionId = q.CorrectOptionId THEN 1 ELSE 0 END,
                    a.QuestionVersionNumber = 1
                FROM QuizAnswers a
                INNER JOIN QuizQuestions q ON q.Id = a.QuestionId
                LEFT JOIN QuizOptions selectedOption ON selectedOption.Id = a.SelectedOptionId
                LEFT JOIN QuizOptions correctOption ON correctOption.Id = q.CorrectOptionId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionVersions");

            migrationBuilder.DropColumn(
                name: "AcceptedAnswersJson",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "CorrectOptionIdsJson",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "CorrectAnswerSnapshot",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionTextSnapshot",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionVersionNumber",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "SelectedAnswerSnapshot",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "SelectedOptionIdsJson",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "QuizAnswers");
        }
    }
}
