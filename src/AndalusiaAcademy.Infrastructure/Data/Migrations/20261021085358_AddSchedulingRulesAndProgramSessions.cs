using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingRulesAndProgramSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "TrainingLocations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE location
                SET Capacity = CASE
                    WHEN offeringCapacity.MaximumCapacity IS NULL THEN 30
                    WHEN offeringCapacity.MaximumCapacity < 1 THEN 1
                    ELSE offeringCapacity.MaximumCapacity
                END
                FROM TrainingLocations AS location
                OUTER APPLY
                (
                    SELECT MAX(capacities.Capacity) AS MaximumCapacity
                    FROM
                    (
                        SELECT Capacity FROM CourseOfferings WHERE LocationId = location.Id
                        UNION ALL
                        SELECT Capacity FROM ProgramOfferings WHERE LocationId = location.Id
                    ) AS capacities
                ) AS offeringCapacity;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "TrainingLocations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "ProgramOfferings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "CourseSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "CourseOfferings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "ProgramSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstructorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReschedulingNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramSessions", x => x.Id);
                    table.CheckConstraint("CK_ProgramSessions_Cancellation", "([Status] = 'Cancelled' AND [CancellationReason] IS NOT NULL) OR ([Status] <> 'Cancelled' AND [CancellationReason] IS NULL)");
                    table.CheckConstraint("CK_ProgramSessions_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
                    table.CheckConstraint("CK_ProgramSessions_Status", "[Status] IN ('Scheduled', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_ProgramSessions_AspNetUsers_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramSessions_ProgramOfferings_ProgramOfferingId",
                        column: x => x.ProgramOfferingId,
                        principalTable: "ProgramOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramSessions_TrainingLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TrainingLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingLocations_Capacity",
                table: "TrainingLocations",
                sql: "[Capacity] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessions_InstructorUserId_StartsAtUtc_EndsAtUtc",
                table: "ProgramSessions",
                columns: new[] { "InstructorUserId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessions_LocationId_StartsAtUtc_EndsAtUtc",
                table: "ProgramSessions",
                columns: new[] { "LocationId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessions_ProgramOfferingId_StartsAtUtc",
                table: "ProgramSessions",
                columns: new[] { "ProgramOfferingId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSessions_Status_StartsAtUtc",
                table: "ProgramSessions",
                columns: new[] { "Status", "StartsAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingLocations_Capacity",
                table: "TrainingLocations");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "TrainingLocations");

            // Reverting LocationId to NOT NULL is only safe if nothing actually relies on it being
            // null (e.g. an online offering with no physical location). Rather than silently
            // assigning a dangling all-zero Guid that no TrainingLocation row actually has — which
            // either fails loudly on the FK (if enforced) or corrupts the reference (if not) — fail
            // loudly and explicitly here, so a human resolves the data before rolling back.
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM ProgramOfferings WHERE LocationId IS NULL)
                    THROW 51000, 'Cannot roll back AddSchedulingRulesAndProgramSessions: ProgramOfferings has rows with a NULL LocationId (e.g. online offerings). Assign a real location to these rows before downgrading.', 1;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "ProgramOfferings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM CourseSessions WHERE LocationId IS NULL)
                    THROW 51000, 'Cannot roll back AddSchedulingRulesAndProgramSessions: CourseSessions has rows with a NULL LocationId (e.g. online sessions). Assign a real location to these rows before downgrading.', 1;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "CourseSessions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM CourseOfferings WHERE LocationId IS NULL)
                    THROW 51000, 'Cannot roll back AddSchedulingRulesAndProgramSessions: CourseOfferings has rows with a NULL LocationId (e.g. online offerings). Assign a real location to these rows before downgrading.', 1;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "CourseOfferings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
