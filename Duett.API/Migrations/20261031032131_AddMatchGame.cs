using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameQuestionOptions", x => x.Id);
                    table.CheckConstraint("CK_GameQuestionOptions_SortOrder", "[SortOrder] >= 1");
                    table.ForeignKey(
                        name: "FK_GameQuestionOptions_GameQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "GameQuestions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GameRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRounds", x => x.Id);
                    table.CheckConstraint("CK_GameRounds_RoundNumber", "[RoundNumber] >= 1");
                    table.ForeignKey(
                        name: "FK_GameRounds_GameQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "GameQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameRounds_UserMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "UserMatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GameRoundSubmissions",
                columns: table => new
                {
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AnswerOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuessOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRoundSubmissions", x => new { x.RoundId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GameRoundSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameRoundSubmissions_GameQuestionOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "GameQuestionOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameRoundSubmissions_GameQuestionOptions_GuessOptionId",
                        column: x => x.GuessOptionId,
                        principalTable: "GameQuestionOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameRoundSubmissions_GameRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "GameRounds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameQuestionOptions_QuestionId_SortOrder",
                table: "GameQuestionOptions",
                columns: new[] { "QuestionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_MatchId_QuestionId",
                table: "GameRounds",
                columns: new[] { "MatchId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_MatchId_RoundNumber",
                table: "GameRounds",
                columns: new[] { "MatchId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_QuestionId",
                table: "GameRounds",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoundSubmissions_AnswerOptionId",
                table: "GameRoundSubmissions",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoundSubmissions_GuessOptionId",
                table: "GameRoundSubmissions",
                column: "GuessOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoundSubmissions_UserId",
                table: "GameRoundSubmissions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameRoundSubmissions");

            migrationBuilder.DropTable(
                name: "GameQuestionOptions");

            migrationBuilder.DropTable(
                name: "GameRounds");

            migrationBuilder.DropTable(
                name: "GameQuestions");
        }
    }
}
