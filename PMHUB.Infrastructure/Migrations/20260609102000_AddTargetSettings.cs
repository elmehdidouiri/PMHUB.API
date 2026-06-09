using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyTargetSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoursPerDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualHoursTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkingDaysPerMonth = table.Column<int>(type: "int", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTargetSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiTargetSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiTargetSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KpiTargetSettings_Name",
                table: "KpiTargetSettings",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyTargetSettings");

            migrationBuilder.DropTable(
                name: "KpiTargetSettings");
        }
    }
}
