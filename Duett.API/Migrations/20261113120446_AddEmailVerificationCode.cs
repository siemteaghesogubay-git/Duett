using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiresAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationFailedAttempts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationNextSendAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedEmailVerificationCode",
                table: "AspNetUsers",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiresAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationFailedAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationNextSendAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProtectedEmailVerificationCode",
                table: "AspNetUsers");
        }
    }
}
