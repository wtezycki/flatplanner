using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flatplanner.Core.Migrations
{
    /// <inheritdoc />
    public partial class AiDossier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_summary",
                table: "listings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "nearby_json",
                table: "listings",
                type: "json",
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "risks_json",
                table: "listings",
                type: "json",
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_summary",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "nearby_json",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "risks_json",
                table: "listings");
        }
    }
}
