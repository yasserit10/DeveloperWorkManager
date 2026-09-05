using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveloperWorkManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDevelopers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectDevelopers",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DeveloperId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDevelopers", x => new { x.ProjectId, x.DeveloperId });
                    table.ForeignKey(
                        name: "FK_ProjectDevelopers_AspNetUsers_DeveloperId",
                        column: x => x.DeveloperId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDevelopers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDevelopers_DeveloperId",
                table: "ProjectDevelopers",
                column: "DeveloperId");

            // Preserve every existing project's responsible developer as its
            // first assignment before the application starts using the new
            // multi-developer relationship.
            migrationBuilder.Sql("""
                INSERT INTO [ProjectDevelopers] ([ProjectId], [DeveloperId], [AssignedAt])
                SELECT [Id], [AssignedDeveloperId], SYSUTCDATETIME()
                FROM [Projects]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [ProjectDevelopers]
                    WHERE [ProjectDevelopers].[ProjectId] = [Projects].[Id]
                      AND [ProjectDevelopers].[DeveloperId] = [Projects].[AssignedDeveloperId]
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDevelopers");
        }
    }
}
