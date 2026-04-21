using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DailyBookingUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_UserId_ProjectId_Date",
                table: "HourEntries",
                columns: new[] { "UserId", "ProjectId", "Date" },
                unique: true,
                filter: "[AllocationType] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HourEntries_UserId_ProjectId_Date",
                table: "HourEntries");
        }
    }
}
