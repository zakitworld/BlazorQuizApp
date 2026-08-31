using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class AddManageableQuizCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizCategories_Name",
                table: "QuizCategories",
                column: "Name",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO QuizCategories (Name, IsActive, SortOrder, CreatedAt)
                SELECT source.Name, 1, source.SortOrder, GETUTCDATE()
                FROM (VALUES
                    ('General', 10), ('ASP.NET Core', 20), ('Blazor', 30), ('C#', 40),
                    ('Database', 50), ('Entity Framework', 60), ('JavaScript', 70),
                    ('Programming Concepts', 80), ('Web Development', 90)
                ) source(Name, SortOrder)
                WHERE NOT EXISTS (SELECT 1 FROM QuizCategories existing WHERE existing.Name = source.Name);

                INSERT INTO QuizCategories (Name, IsActive, SortOrder, CreatedAt)
                SELECT DISTINCT questions.Category, 1, 100, GETUTCDATE()
                FROM QuizQuestions questions
                WHERE questions.Category IS NOT NULL AND LTRIM(RTRIM(questions.Category)) <> ''
                  AND NOT EXISTS (SELECT 1 FROM QuizCategories existing WHERE existing.Name = questions.Category);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizCategories");
        }
    }
}
