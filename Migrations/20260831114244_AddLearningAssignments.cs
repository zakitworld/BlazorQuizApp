using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "QuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CertificateCode",
                table: "QuizResults",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "QuizResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuizAssignmentId",
                table: "QuizResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuizTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    PassPercentage = table.Column<int>(type: "int", nullable: false),
                    IsAdaptive = table.Column<bool>(type: "bit", nullable: false),
                    RandomizeQuestions = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizTemplateQuestions",
                columns: table => new
                {
                    QuizTemplateId = table.Column<int>(type: "int", nullable: false),
                    QuizQuestionId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizTemplateQuestions", x => new { x.QuizTemplateId, x.QuizQuestionId });
                    table.ForeignKey(
                        name: "FK_QuizTemplateQuestions_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizTemplateQuestions_QuizTemplates_QuizTemplateId",
                        column: x => x.QuizTemplateId,
                        principalTable: "QuizTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    QuizTemplateId = table.Column<int>(type: "int", nullable: false),
                    UserGroupId = table.Column<int>(type: "int", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AccessCodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccessCodeHint = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAssignments_QuizTemplates_QuizTemplateId",
                        column: x => x.QuizTemplateId,
                        principalTable: "QuizTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizAssignments_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupMembers",
                columns: table => new
                {
                    UserGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupMembers", x => new { x.UserGroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserGroupMembers_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentEnrollments",
                columns: table => new
                {
                    QuizAssignmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentEnrollments", x => new { x.QuizAssignmentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AssignmentEnrollments_QuizAssignments_QuizAssignmentId",
                        column: x => x.QuizAssignmentId,
                        principalTable: "QuizAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_QuizAssignmentId_UserId_AttemptNumber",
                table: "QuizResults",
                columns: new[] { "QuizAssignmentId", "UserId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAssignments_AccessCodeHash",
                table: "QuizAssignments",
                column: "AccessCodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAssignments_QuizTemplateId",
                table: "QuizAssignments",
                column: "QuizTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAssignments_UserGroupId",
                table: "QuizAssignments",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizTemplateQuestions_QuizQuestionId",
                table: "QuizTemplateQuestions",
                column: "QuizQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizResults_QuizAssignments_QuizAssignmentId",
                table: "QuizResults",
                column: "QuizAssignmentId",
                principalTable: "QuizAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizResults_QuizAssignments_QuizAssignmentId",
                table: "QuizResults");

            migrationBuilder.DropTable(
                name: "AssignmentEnrollments");

            migrationBuilder.DropTable(
                name: "QuizTemplateQuestions");

            migrationBuilder.DropTable(
                name: "UserGroupMembers");

            migrationBuilder.DropTable(
                name: "QuizAssignments");

            migrationBuilder.DropTable(
                name: "QuizTemplates");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropIndex(
                name: "IX_QuizResults_QuizAssignmentId_UserId_AttemptNumber",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "CertificateCode",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "QuizAssignmentId",
                table: "QuizResults");
        }
    }
}
