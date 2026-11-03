using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseMaterialFileUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ResourceUrl",
                table: "CourseMaterials",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AddColumn<string>(
                name: "FileContentType",
                table: "CourseMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "CourseMaterials",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "CourseMaterials",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                table: "CourseMaterials",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Link");

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "CourseMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseMaterials_ResourceShape",
                table: "CourseMaterials",
                sql: "([ResourceType] = 'Link' AND [ResourceUrl] IS NOT NULL AND [OriginalFileName] IS NULL AND [StoredFileName] IS NULL AND [FileContentType] IS NULL AND [FileSizeBytes] IS NULL) OR ([ResourceType] = 'File' AND [ResourceUrl] IS NULL AND [OriginalFileName] IS NOT NULL AND [StoredFileName] IS NOT NULL AND [FileContentType] IS NOT NULL AND [FileSizeBytes] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseMaterials_ResourceType",
                table: "CourseMaterials",
                sql: "[ResourceType] IN ('Link', 'File')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseMaterials_ResourceShape",
                table: "CourseMaterials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseMaterials_ResourceType",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "FileContentType",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "CourseMaterials");

            migrationBuilder.AlterColumn<string>(
                name: "ResourceUrl",
                table: "CourseMaterials",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
