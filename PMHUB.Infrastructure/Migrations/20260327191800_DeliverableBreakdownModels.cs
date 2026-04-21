using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeliverableBreakdownModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliverableBreakdowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverableBreakdowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverableBreakdowns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliverableTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliverableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProjectMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InternAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverableTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverableTasks_DeliverableBreakdowns_DeliverableId",
                        column: x => x.DeliverableId,
                        principalTable: "DeliverableBreakdowns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverableTasks_InternAllocations_InternAllocationId",
                        column: x => x.InternAllocationId,
                        principalTable: "InternAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliverableTasks_ProjectMembers_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalTable: "ProjectMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableBreakdowns_ProjectId",
                table: "DeliverableBreakdowns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableTasks_DeliverableId",
                table: "DeliverableTasks",
                column: "DeliverableId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableTasks_InternAllocationId",
                table: "DeliverableTasks",
                column: "InternAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableTasks_ProjectMemberId",
                table: "DeliverableTasks",
                column: "ProjectMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliverableTasks");

            migrationBuilder.DropTable(
                name: "DeliverableBreakdowns");
        }
    }
}
