using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SecondUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMatches", x => x.Id);
                    table.CheckConstraint("CK_UserMatches_UserOrder", "[FirstUserId] COLLATE Latin1_General_100_BIN2 < [SecondUserId] COLLATE Latin1_General_100_BIN2");
                    table.ForeignKey(
                        name: "FK_UserMatches_AspNetUsers_FirstUserId",
                        column: x => x.FirstUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMatches_AspNetUsers_SecondUserId",
                        column: x => x.SecondUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMatches_FirstUserId_SecondUserId",
                table: "UserMatches",
                columns: new[] { "FirstUserId", "SecondUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMatches_SecondUserId",
                table: "UserMatches",
                column: "SecondUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMatches");
        }
    }
}
