using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flatplanner.Core.Migrations
{
    /// <inheritdoc />
    public partial class DescriptionMarkup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description_markup",
                table: "listings",
                type: "mediumtext",
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_markup",
                table: "listings");
        }
    }
}
