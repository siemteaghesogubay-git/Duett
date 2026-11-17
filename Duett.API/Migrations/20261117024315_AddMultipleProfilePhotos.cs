using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations;

public partial class AddMultipleProfilePhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProfilePhotos_AspNetUsers_UserId",
            table: "ProfilePhotos");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ProfilePhotos",
            table: "ProfilePhotos");

        migrationBuilder.DropIndex(
            name: "IX_ProfilePhotos_StorageKey",
            table: "ProfilePhotos");

        migrationBuilder.DropCheckConstraint(
            name: "CK_ProfilePhotos_SizeBytes",
            table: "ProfilePhotos");

        migrationBuilder.AddColumn<Guid>(
            name: "Id",
            table: "ProfilePhotos",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE [ProfilePhotos] " +
            "SET [Id] = NEWID() " +
            "WHERE [Id] IS NULL;");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "ProfilePhotos",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SortOrder",
            table: "ProfilePhotos",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddPrimaryKey(
            name: "PK_ProfilePhotos",
            table: "ProfilePhotos",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_ProfilePhotos_UserId_SortOrder",
            table: "ProfilePhotos",
            columns: new[]
            {
                "UserId",
                "SortOrder"
            },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_ProfilePhotos_AspNetUsers_UserId",
            table: "ProfilePhotos",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProfilePhotos_AspNetUsers_UserId",
            table: "ProfilePhotos");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ProfilePhotos",
            table: "ProfilePhotos");

        migrationBuilder.DropIndex(
            name: "IX_ProfilePhotos_UserId_SortOrder",
            table: "ProfilePhotos");

        migrationBuilder.DropColumn(
            name: "Id",
            table: "ProfilePhotos");

        migrationBuilder.DropColumn(
            name: "SortOrder",
            table: "ProfilePhotos");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ProfilePhotos",
            table: "ProfilePhotos",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ProfilePhotos_StorageKey",
            table: "ProfilePhotos",
            column: "StorageKey",
            unique: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_ProfilePhotos_SizeBytes",
            table: "ProfilePhotos",
            sql: "[SizeBytes] > 0");

        migrationBuilder.AddForeignKey(
            name: "FK_ProfilePhotos_AspNetUsers_UserId",
            table: "ProfilePhotos",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id");
    }
}