using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentSubmissionLinksAndFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileContentType",
                table: "AssignmentSubmissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "AssignmentSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "AssignmentSubmissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "AssignmentSubmissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "AssignmentSubmissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionType",
                table: "AssignmentSubmissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Text");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssignmentSubmissions_ContentShape",
                table: "AssignmentSubmissions",
                sql: "([SubmissionType] = 'Text' AND [LinkUrl] IS NULL AND [StoredFileName] IS NULL) OR ([SubmissionType] = 'Link' AND [LinkUrl] IS NOT NULL AND [StoredFileName] IS NULL) OR ([SubmissionType] = 'File' AND [LinkUrl] IS NULL AND [OriginalFileName] IS NOT NULL AND [StoredFileName] IS NOT NULL AND [FileContentType] IS NOT NULL AND [FileSizeBytes] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssignmentSubmissions_SubmissionType",
                table: "AssignmentSubmissions",
                sql: "[SubmissionType] IN ('Text', 'Link', 'File')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssignmentSubmissions_ContentShape",
                table: "AssignmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssignmentSubmissions_SubmissionType",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "FileContentType",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmissionType",
                table: "AssignmentSubmissions");
        }
    }
}
