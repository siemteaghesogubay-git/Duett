using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duett.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: false),
                    MaxAge = table.Column<int>(type: "int", nullable: false),
                    PreferredGenders = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelationshipGoal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerPreferences", x => x.UserId);
                    table.CheckConstraint("CK_PartnerPreferences_AgeRange", "[MinAge] >= 18 AND [MaxAge] >= [MinAge]");
                    table.ForeignKey(
                        name: "FK_PartnerPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerPreferences");
        }
    }
}
