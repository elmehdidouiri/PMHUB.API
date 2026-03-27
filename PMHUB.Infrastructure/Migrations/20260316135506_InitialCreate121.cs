using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate121 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "IsPremiumApproved",
                table: "HourEntries");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "HourEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "HourEntries",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRateAmount",
                table: "HourEntries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PremiumApprovalStatus",
                table: "HourEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PremiumReason",
                table: "HourEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "HourEntries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "WeekBatchId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserHourlyRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalRateAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PremiumRateAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHourlyRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserHourlyRates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_IsPremium_PremiumApprovalStatus",
                table: "HourEntries",
                columns: new[] { "IsPremium", "PremiumApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_WeekBatchId",
                table: "HourEntries",
                column: "WeekBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date_Country",
                table: "Holidays",
                columns: new[] { "Date", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_UserHourlyRates_UserId_EffectiveFrom",
                table: "UserHourlyRates",
                columns: new[] { "UserId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "UserHourlyRates");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_IsPremium_PremiumApprovalStatus",
                table: "HourEntries");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_WeekBatchId",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "HourlyRateAmount",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "PremiumApprovalStatus",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "PremiumReason",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "WeekBatchId",
                table: "HourEntries");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "HourEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremiumApproved",
                table: "HourEntries",
                type: "bit",
                nullable: true);
        }
    }
}
