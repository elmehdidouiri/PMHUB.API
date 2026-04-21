using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate11200000110000 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "DeliverableTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DevHours",
                table: "DeliverableTasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Hours",
                table: "DeliverableTasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryPoints",
                table: "DeliverableTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TestingHours",
                table: "DeliverableTasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UxHours",
                table: "DeliverableTasks",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "DeliverableTasks");

            migrationBuilder.DropColumn(
                name: "DevHours",
                table: "DeliverableTasks");

            migrationBuilder.DropColumn(
                name: "Hours",
                table: "DeliverableTasks");

            migrationBuilder.DropColumn(
                name: "StoryPoints",
                table: "DeliverableTasks");

            migrationBuilder.DropColumn(
                name: "TestingHours",
                table: "DeliverableTasks");

            migrationBuilder.DropColumn(
                name: "UxHours",
                table: "DeliverableTasks");
        }
    }
}
