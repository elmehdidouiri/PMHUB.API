using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedStartDateToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedStartDate",
                table: "Projects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedStartDate",
                table: "Projects");
        }
    }
}
