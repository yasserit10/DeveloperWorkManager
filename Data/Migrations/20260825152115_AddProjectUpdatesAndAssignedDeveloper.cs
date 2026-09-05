using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveloperWorkManager.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectUpdatesAndAssignedDeveloper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TargetDate",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "AssignedDeveloperId",
                table: "Projects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Projects
                SET AssignedDeveloperId = COALESCE(
                    (SELECT TOP(1) AssignedToId FROM WorkItems WHERE WorkItems.ProjectId = Projects.Id ORDER BY CreatedAt),
                    (SELECT TOP(1) Id FROM AspNetUsers WHERE IsArchived = 0 ORDER BY UserName)
                )
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AssignedDeveloperId",
                table: "Projects",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectUpdates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectUpdates_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectUpdates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_AssignedDeveloperId",
                table: "Projects",
                column: "AssignedDeveloperId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUpdates_CreatedById",
                table: "ProjectUpdates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUpdates_ProjectId_CreatedAt",
                table: "ProjectUpdates",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_AssignedDeveloperId",
                table: "Projects",
                column: "AssignedDeveloperId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_AssignedDeveloperId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectUpdates");

            migrationBuilder.DropIndex(
                name: "IX_Projects_AssignedDeveloperId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AssignedDeveloperId",
                table: "Projects");

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TargetDate",
                table: "Projects",
                type: "date",
                nullable: true);
        }
    }
}
