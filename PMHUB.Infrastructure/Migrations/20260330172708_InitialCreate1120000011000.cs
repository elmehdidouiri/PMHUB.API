using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate1120000011000 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Interns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Interns_RoleId",
                table: "Interns",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interns_Roles_RoleId",
                table: "Interns",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interns_Roles_RoleId",
                table: "Interns");

            migrationBuilder.DropIndex(
                name: "IX_Interns_RoleId",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Interns");
        }
    }
}
