using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveloperWorkManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProjectCodeWithLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_Code",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Projects",
                newName: "ProjectUrl");

            migrationBuilder.AlterColumn<string>(
                name: "ProjectUrl",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "LatestReleaseUrl",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestReleaseUrl",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "ProjectUrl",
                table: "Projects",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "ProjectUrl",
                table: "Projects",
                newName: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);
        }
    }
}
