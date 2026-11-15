using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261106143000_ProtectApplicationAndEnrollmentInvariants")]
public partial class ProtectApplicationAndEnrollmentInvariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IntegrityProtected",
            table: "LearnerApplications",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "IntegrityProtected",
            table: "Enrollments",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.DropIndex(
            name: "IX_LearnerApplications_User_CourseOffering_Pending",
            table: "LearnerApplications");

        migrationBuilder.DropIndex(
            name: "IX_LearnerApplications_User_ProgramOffering_Pending",
            table: "LearnerApplications");

        // Earlier versions allowed duplicate accepted applications and active enrollments. Preserve
        // every legacy record exactly as-is, but mark one deterministic row in each legacy group as
        // protected. New rows default to protected, so the indexes prevent future duplicates without
        // rewriting historical application or enrollment statuses.
        migrationBuilder.Sql(
            """
            ;WITH RankedCourseEnrollments AS
            (
                SELECT [Id], ROW_NUMBER() OVER
                (
                    PARTITION BY [UserId], [CourseOfferingId]
                    ORDER BY [EnrolledAt], [Id]
                ) AS [RowNumber]
                FROM [Enrollments]
                WHERE [Status] = 'Active' AND [CourseOfferingId] IS NOT NULL
            )
            UPDATE enrollment
            SET [IntegrityProtected] = 0
            FROM [Enrollments] AS enrollment
            INNER JOIN RankedCourseEnrollments AS ranked ON ranked.[Id] = enrollment.[Id]
            WHERE ranked.[RowNumber] > 1;

            ;WITH RankedProgramEnrollments AS
            (
                SELECT [Id], ROW_NUMBER() OVER
                (
                    PARTITION BY [UserId], [ProgramOfferingId]
                    ORDER BY [EnrolledAt], [Id]
                ) AS [RowNumber]
                FROM [Enrollments]
                WHERE [Status] = 'Active' AND [ProgramOfferingId] IS NOT NULL
            )
            UPDATE enrollment
            SET [IntegrityProtected] = 0
            FROM [Enrollments] AS enrollment
            INNER JOIN RankedProgramEnrollments AS ranked ON ranked.[Id] = enrollment.[Id]
            WHERE ranked.[RowNumber] > 1;

            ;WITH RankedCourseApplications AS
            (
                SELECT application.[Id], ROW_NUMBER() OVER
                (
                    PARTITION BY application.[UserId], application.[CourseOfferingId]
                    ORDER BY
                        CASE WHEN EXISTS
                        (
                            SELECT 1
                            FROM [Enrollments] AS enrollment
                            WHERE enrollment.[ApplicationId] = application.[Id]
                                AND enrollment.[Status] = 'Active'
                        ) THEN 0 ELSE 1 END,
                        CASE WHEN application.[Status] = 'Accepted' THEN 0 ELSE 1 END,
                        application.[SubmittedAt],
                        application.[Id]
                ) AS [RowNumber]
                FROM [LearnerApplications] AS application
                WHERE application.[Status] IN ('Pending', 'Accepted')
                    AND application.[CourseOfferingId] IS NOT NULL
            )
            UPDATE application
            SET [IntegrityProtected] = 0
            FROM [LearnerApplications] AS application
            INNER JOIN RankedCourseApplications AS ranked ON ranked.[Id] = application.[Id]
            WHERE ranked.[RowNumber] > 1;

            ;WITH RankedProgramApplications AS
            (
                SELECT application.[Id], ROW_NUMBER() OVER
                (
                    PARTITION BY application.[UserId], application.[ProgramOfferingId]
                    ORDER BY
                        CASE WHEN EXISTS
                        (
                            SELECT 1
                            FROM [Enrollments] AS enrollment
                            WHERE enrollment.[ApplicationId] = application.[Id]
                                AND enrollment.[Status] = 'Active'
                        ) THEN 0 ELSE 1 END,
                        CASE WHEN application.[Status] = 'Accepted' THEN 0 ELSE 1 END,
                        application.[SubmittedAt],
                        application.[Id]
                ) AS [RowNumber]
                FROM [LearnerApplications] AS application
                WHERE application.[Status] IN ('Pending', 'Accepted')
                    AND application.[ProgramOfferingId] IS NOT NULL
            )
            UPDATE application
            SET [IntegrityProtected] = 0
            FROM [LearnerApplications] AS application
            INNER JOIN RankedProgramApplications AS ranked ON ranked.[Id] = application.[Id]
            WHERE ranked.[RowNumber] > 1;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_LearnerApplications_User_CourseOffering_Open",
            table: "LearnerApplications",
            columns: new[] { "UserId", "CourseOfferingId" },
            unique: true,
            filter: "[IntegrityProtected] = 1 AND [Status] IN ('Pending', 'Accepted') AND [CourseOfferingId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_LearnerApplications_User_ProgramOffering_Open",
            table: "LearnerApplications",
            columns: new[] { "UserId", "ProgramOfferingId" },
            unique: true,
            filter: "[IntegrityProtected] = 1 AND [Status] IN ('Pending', 'Accepted') AND [ProgramOfferingId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollments_User_CourseOffering_Active",
            table: "Enrollments",
            columns: new[] { "UserId", "CourseOfferingId" },
            unique: true,
            filter: "[IntegrityProtected] = 1 AND [Status] = 'Active' AND [CourseOfferingId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollments_User_ProgramOffering_Active",
            table: "Enrollments",
            columns: new[] { "UserId", "ProgramOfferingId" },
            unique: true,
            filter: "[IntegrityProtected] = 1 AND [Status] = 'Active' AND [ProgramOfferingId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Enrollments_User_CourseOffering_Active",
            table: "Enrollments");

        migrationBuilder.DropIndex(
            name: "IX_Enrollments_User_ProgramOffering_Active",
            table: "Enrollments");

        migrationBuilder.DropIndex(
            name: "IX_LearnerApplications_User_CourseOffering_Open",
            table: "LearnerApplications");

        migrationBuilder.DropIndex(
            name: "IX_LearnerApplications_User_ProgramOffering_Open",
            table: "LearnerApplications");

        migrationBuilder.DropColumn(
            name: "IntegrityProtected",
            table: "Enrollments");

        migrationBuilder.DropColumn(
            name: "IntegrityProtected",
            table: "LearnerApplications");

        migrationBuilder.CreateIndex(
            name: "IX_LearnerApplications_User_CourseOffering_Pending",
            table: "LearnerApplications",
            columns: new[] { "UserId", "CourseOfferingId" },
            unique: true,
            filter: "[Status] = 'Pending' AND [CourseOfferingId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_LearnerApplications_User_ProgramOffering_Pending",
            table: "LearnerApplications",
            columns: new[] { "UserId", "ProgramOfferingId" },
            unique: true,
            filter: "[Status] = 'Pending' AND [ProgramOfferingId] IS NOT NULL");
    }
}
