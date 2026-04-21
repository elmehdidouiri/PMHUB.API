using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate1120000001100000000 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InternManagementHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "HourEntryInternSupervisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HourEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourEntryInternSupervisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourEntryInternSupervisions_HourEntries_HourEntryId",
                        column: x => x.HourEntryId,
                        principalTable: "HourEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HourEntryInternSupervisions_InternAllocations_InternAllocationId",
                        column: x => x.InternAllocationId,
                        principalTable: "InternAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourEntryInternSupervisions_HourEntryId",
                table: "HourEntryInternSupervisions",
                column: "HourEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_HourEntryInternSupervisions_InternAllocationId",
                table: "HourEntryInternSupervisions",
                column: "InternAllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HourEntryInternSupervisions");

            migrationBuilder.DropColumn(
                name: "InternManagementHours",
                table: "HourEntries");
        }
    }
}
