using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate0000000000001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "Users");
        }
    }
}
