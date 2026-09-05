using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveloperWorkManager.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemoNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MemoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImageFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    AssignedToId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoRequests_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemoRequests_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemoRequests_AssignedToId_MemoDate",
                table: "MemoRequests",
                columns: new[] { "AssignedToId", "MemoDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoRequests_CreatedAt",
                table: "MemoRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemoRequests_CreatedById",
                table: "MemoRequests",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoRequests");
        }
    }
}
