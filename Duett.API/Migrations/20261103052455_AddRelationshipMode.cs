using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelationshipMode",
                table: "PartnerPreferences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationshipMode",
                table: "PartnerPreferences");
        }
    }
}
