using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnerApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerApplications", x => x.Id);
                    table.CheckConstraint("CK_LearnerApplications_ExactlyOneTarget", "([CourseId] IS NOT NULL AND [ProgramId] IS NULL) OR ([CourseId] IS NULL AND [ProgramId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_LearnerApplications_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearnerApplications_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearnerApplicationStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerApplicationStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerApplicationStatusHistories_LearnerApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "LearnerApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerApplications_CourseId",
                table: "LearnerApplications",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerApplications_ProgramId",
                table: "LearnerApplications",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerApplications_Status",
                table: "LearnerApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerApplications_User_Course_Pending",
                table: "LearnerApplications",
                columns: new[] { "UserId", "CourseId" },
                unique: true,
                filter: "[Status] = 'Pending' AND [CourseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerApplications_User_Program_Pending",
                table: "LearnerApplications",
                columns: new[] { "UserId", "ProgramId" },
                unique: true,
                filter: "[Status] = 'Pending' AND [ProgramId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerApplicationStatusHistories_ApplicationId",
                table: "LearnerApplicationStatusHistories",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerApplicationStatusHistories");

            migrationBuilder.DropTable(
                name: "LearnerApplications");
        }
    }
}
