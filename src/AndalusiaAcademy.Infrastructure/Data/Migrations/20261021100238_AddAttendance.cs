using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseSessionAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessionAttendances", x => x.Id);
                    table.CheckConstraint("CK_CourseSessionAttendances_Status", "[Status] IN ('Present', 'Absent', 'Late', 'Excused')");
                    table.ForeignKey(
                        name: "FK_CourseSessionAttendances_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSessionAttendances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSessionAttendances_CourseSessions_CourseSessionId",
                        column: x => x.CourseSessionId,
                        principalTable: "CourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramSessionAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramSessionAttendances", x => x.Id);
                    table.CheckConstraint("CK_ProgramSessionAttendances_Status", "[Status] IN ('Present', 'Absent', 'Late', 'Excused')");
                    table.ForeignKey(
                        name: "FK_ProgramSessionAttendances_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramSessionAttendances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramSessionAttendances_ProgramSessions_ProgramSessionId",
                        column: x => x.ProgramSessionId,
                        principalTable: "ProgramSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionAttendances_CourseSessionId_UserId",
                table: "CourseSessionAttendances",
                columns: new[] { "CourseSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionAttendances_RecordedByUserId",
                table: "CourseSessionAttendances",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionAttendances_UserId",
                table: "CourseSessionAttendances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessionAttendances_ProgramSessionId_UserId",
                table: "ProgramSessionAttendances",
                columns: new[] { "ProgramSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessionAttendances_RecordedByUserId",
                table: "ProgramSessionAttendances",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessionAttendances_UserId",
                table: "ProgramSessionAttendances",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSessionAttendances");

            migrationBuilder.DropTable(
                name: "ProgramSessionAttendances");
        }
    }
}
