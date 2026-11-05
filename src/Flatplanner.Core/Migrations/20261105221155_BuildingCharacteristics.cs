using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flatplanner.Core.Migrations
{
    /// <inheritdoc />
    public partial class BuildingCharacteristics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "building_material",
                table: "listings",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "building_type",
                table: "listings",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "heating",
                table: "listings",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "windows_type",
                table: "listings",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "building_material",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "building_type",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "heating",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "windows_type",
                table: "listings");
        }
    }
}
