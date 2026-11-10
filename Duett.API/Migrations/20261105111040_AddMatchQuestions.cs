using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AskedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnsweredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchQuestions", x => x.Id);
                    table.CheckConstraint("CK_MatchQuestions_AnswerState", "([AnswerText] IS NULL AND [AnsweredByUserId] IS NULL AND [AnsweredAtUtc] IS NULL) OR ([AnswerText] IS NOT NULL AND [AnsweredByUserId] IS NOT NULL AND [AnsweredAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_MatchQuestions_NoSelfAnswer", "[AnsweredByUserId] IS NULL OR [AnsweredByUserId] <> [AskedByUserId]");
                    table.CheckConstraint("CK_MatchQuestions_QuestionNumber", "[QuestionNumber] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_MatchQuestions_AspNetUsers_AnsweredByUserId",
                        column: x => x.AnsweredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchQuestions_AspNetUsers_AskedByUserId",
                        column: x => x.AskedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchQuestions_UserMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "UserMatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestions_AnsweredByUserId",
                table: "MatchQuestions",
                column: "AnsweredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestions_AskedByUserId",
                table: "MatchQuestions",
                column: "AskedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestions_MatchId_AskedByUserId_QuestionNumber",
                table: "MatchQuestions",
                columns: new[] { "MatchId", "AskedByUserId", "QuestionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchQuestions");
        }
    }
}
