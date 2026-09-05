using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveloperWorkManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkStateTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceWorkItemId",
                table: "Achievements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkStateEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WorkItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkStateEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkStateEntries_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkStateEntries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkStateEntries_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_SourceWorkItemId",
                table: "Achievements",
                column: "SourceWorkItemId",
                unique: true,
                filter: "[SourceWorkItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkStateEntries_CreatedById_CreatedAt",
                table: "WorkStateEntries",
                columns: new[] { "CreatedById", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkStateEntries_ProjectId_CreatedAt",
                table: "WorkStateEntries",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkStateEntries_WorkItemId",
                table: "WorkStateEntries",
                column: "WorkItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Achievements_WorkItems_SourceWorkItemId",
                table: "Achievements",
                column: "SourceWorkItemId",
                principalTable: "WorkItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievements_WorkItems_SourceWorkItemId",
                table: "Achievements");

            migrationBuilder.DropTable(
                name: "WorkStateEntries");

            migrationBuilder.DropIndex(
                name: "IX_Achievements_SourceWorkItemId",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "SourceWorkItemId",
                table: "Achievements");
        }
    }
}
