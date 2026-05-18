using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportNonProjectHourActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_UserId_ProjectId_Date",
                table: "HourEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "HourEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE h
                SET ProjectId = NULL,
                    Category = CASE WHEN WorkshopHours > 0 THEN 2 ELSE 4 END
                FROM HourEntries h
                WHERE h.ProjectId = '00000000-0000-0000-0000-000000000000'
                   OR NOT EXISTS (
                       SELECT 1
                       FROM Projects p
                       WHERE p.Id = h.ProjectId
                   )
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_UserId_Category_Date",
                table: "HourEntries",
                columns: new[] { "UserId", "Category", "Date" },
                unique: true,
                filter: "[AllocationType] = 0 AND [ProjectId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_UserId_ProjectId_Date",
                table: "HourEntries",
                columns: new[] { "UserId", "ProjectId", "Date" },
                unique: true,
                filter: "[AllocationType] = 0 AND [ProjectId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_UserId_Category_Date",
                table: "HourEntries");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_UserId_ProjectId_Date",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "HourEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_UserId_ProjectId_Date",
                table: "HourEntries",
                columns: new[] { "UserId", "ProjectId", "Date" },
                unique: true,
                filter: "[AllocationType] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
