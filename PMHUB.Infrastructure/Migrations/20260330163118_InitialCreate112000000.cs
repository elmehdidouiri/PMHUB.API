using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate112000000 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InternAllocations_Interns_InternId",
                table: "InternAllocations");

            migrationBuilder.DropIndex(
                name: "IX_InternAllocations_ProjectId",
                table: "InternAllocations");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "InternAllocations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InternHourEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternHourEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternHourEntries_InternAllocations_InternAllocationId",
                        column: x => x.InternAllocationId,
                        principalTable: "InternAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InternHourEntries_Users_BookedByUserId",
                        column: x => x.BookedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternAllocations_ProjectId_InternId",
                table: "InternAllocations",
                columns: new[] { "ProjectId", "InternId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternHourEntries_BookedByUserId",
                table: "InternHourEntries",
                column: "BookedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternHourEntries_InternAllocationId",
                table: "InternHourEntries",
                column: "InternAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InternHourEntries_InternAllocationId_Date",
                table: "InternHourEntries",
                columns: new[] { "InternAllocationId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_InternAllocations_Interns_InternId",
                table: "InternAllocations",
                column: "InternId",
                principalTable: "Interns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InternAllocations_Interns_InternId",
                table: "InternAllocations");

            migrationBuilder.DropTable(
                name: "InternHourEntries");

            migrationBuilder.DropIndex(
                name: "IX_InternAllocations_ProjectId_InternId",
                table: "InternAllocations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "InternAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_InternAllocations_ProjectId",
                table: "InternAllocations",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_InternAllocations_Interns_InternId",
                table: "InternAllocations",
                column: "InternId",
                principalTable: "Interns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
