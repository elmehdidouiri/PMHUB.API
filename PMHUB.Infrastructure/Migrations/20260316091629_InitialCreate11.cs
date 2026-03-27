using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Sprints_SprintId",
                table: "HourEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Tasks_TaskId",
                table: "HourEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Users_UserId",
                table: "HourEntries");

            migrationBuilder.DropTable(
                name: "AllocationTemplates");

            migrationBuilder.DropTable(
                name: "ProjectAllocations");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_SprintId",
                table: "HourEntries");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_TaskId",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "HourEntries");

            migrationBuilder.RenameColumn(
                name: "Hours",
                table: "HourEntries",
                newName: "WorkshopHours");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExecutionHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "HourEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremiumApproved",
                table: "HourEntries",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManagementHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProcessHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProjectType",
                table: "HourEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RAndDHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupervisionHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalHours",
                table: "HourEntries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_UserId_Date",
                table: "HourEntries",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Users_UserId",
                table: "HourEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_HourEntries_Users_UserId",
                table: "HourEntries");

            migrationBuilder.DropIndex(
                name: "IX_HourEntries_UserId_Date",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "ExecutionHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "FinalHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "IsPremiumApproved",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "ManagementHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "OtherHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "ProcessHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "ProjectType",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "RAndDHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "SupervisionHours",
                table: "HourEntries");

            migrationBuilder.DropColumn(
                name: "TotalHours",
                table: "HourEntries");

            migrationBuilder.RenameColumn(
                name: "WorkshopHours",
                table: "HourEntries",
                newName: "Hours");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SprintId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "HourEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AllocationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DefaultHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocationType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAllocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_SprintId",
                table: "HourEntries",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_HourEntries_TaskId",
                table: "HourEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocations_ProjectId",
                table: "ProjectAllocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocations_UserId",
                table: "ProjectAllocations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Projects_ProjectId",
                table: "HourEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Sprints_SprintId",
                table: "HourEntries",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Tasks_TaskId",
                table: "HourEntries",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HourEntries_Users_UserId",
                table: "HourEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
