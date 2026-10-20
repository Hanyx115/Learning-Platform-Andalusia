using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferingsToApplicationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Courses_CourseId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Programs_ProgramId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerApplications_Courses_CourseId",
                table: "LearnerApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerApplications_Programs_ProgramId",
                table: "LearnerApplications");

            migrationBuilder.DropIndex(
                name: "IX_LearnerApplications_User_Course_Pending",
                table: "LearnerApplications");

            migrationBuilder.DropIndex(
                name: "IX_LearnerApplications_User_Program_Pending",
                table: "LearnerApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearnerApplications_ExactlyOneTarget",
                table: "LearnerApplications");

            migrationBuilder.RenameColumn(
                name: "ProgramId",
                table: "LearnerApplications",
                newName: "ProgramOfferingId");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "LearnerApplications",
                newName: "CourseOfferingId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerApplications_ProgramId",
                table: "LearnerApplications",
                newName: "IX_LearnerApplications_ProgramOfferingId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerApplications_CourseId",
                table: "LearnerApplications",
                newName: "IX_LearnerApplications_CourseOfferingId");

            migrationBuilder.RenameColumn(
                name: "ProgramId",
                table: "Enrollments",
                newName: "ProgramOfferingId");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Enrollments",
                newName: "CourseOfferingId");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_ProgramId",
                table: "Enrollments",
                newName: "IX_Enrollments_ProgramOfferingId");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments",
                newName: "IX_Enrollments_CourseOfferingId");

            // Older local development databases may still contain the offering tables from the
            // discarded catalog-offerings branch. Preserve their rows before replacing that schema
            // with the offering model used by the application flow.
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[CourseOfferings]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'CourseOfferings', N'StartsAt') IS NOT NULL
                BEGIN
                    SELECT Id, CourseId, StartsAt, EndsAt, Price, Currency, Capacity, Status
                    INTO [__LegacyCourseOfferingsImport]
                    FROM [CourseOfferings];

                    DROP TABLE [CourseOfferings];
                END;

                IF OBJECT_ID(N'[ProgramOfferings]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'ProgramOfferings', N'StartsAt') IS NOT NULL
                BEGIN
                    SELECT Id, ProgramId, StartsAt, EndsAt, Price, Currency, Capacity, Status
                    INTO [__LegacyProgramOfferingsImport]
                    FROM [ProgramOfferings];

                    DROP TABLE [ProgramOfferings];
                END;
                """);

            migrationBuilder.CreateTable(
                name: "CourseOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicationOpensAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicationClosesAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferings", x => x.Id);
                    table.CheckConstraint("CK_CourseOfferings_ApplicationWindow", "[ApplicationOpensAtUtc] < [ApplicationClosesAtUtc] AND [ApplicationClosesAtUtc] <= [StartsAtUtc]");
                    table.CheckConstraint("CK_CourseOfferings_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_CourseOfferings_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_CourseOfferings_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
                    table.CheckConstraint("CK_CourseOfferings_Status", "[Status] IN ('Draft', 'Open', 'Closed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_CourseOfferings_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgramOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicationOpensAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicationClosesAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramOfferings", x => x.Id);
                    table.CheckConstraint("CK_ProgramOfferings_ApplicationWindow", "[ApplicationOpensAtUtc] < [ApplicationClosesAtUtc] AND [ApplicationClosesAtUtc] <= [StartsAtUtc]");
                    table.CheckConstraint("CK_ProgramOfferings_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_ProgramOfferings_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_ProgramOfferings_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
                    table.CheckConstraint("CK_ProgramOfferings_Status", "[Status] IN ('Draft', 'Open', 'Closed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_ProgramOfferings_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[__LegacyCourseOfferingsImport]', N'U') IS NOT NULL
                BEGIN
                    INSERT INTO [CourseOfferings]
                        (Id, CourseId, StartsAtUtc, EndsAtUtc, ApplicationOpensAtUtc,
                         ApplicationClosesAtUtc, Price, Currency, Capacity, Status, CreatedAtUtc)
                    SELECT Id, CourseId, StartsAt, EndsAt,
                           DATEADD(day, -30, StartsAt), DATEADD(day, -1, StartsAt),
                           Price, Currency, Capacity,
                           CASE WHEN Status IN ('Draft', 'Open', 'Closed', 'Cancelled')
                                THEN Status ELSE 'Closed' END,
                           SYSDATETIMEOFFSET()
                    FROM [__LegacyCourseOfferingsImport];

                    DROP TABLE [__LegacyCourseOfferingsImport];
                END;

                IF OBJECT_ID(N'[__LegacyProgramOfferingsImport]', N'U') IS NOT NULL
                BEGIN
                    INSERT INTO [ProgramOfferings]
                        (Id, ProgramId, StartsAtUtc, EndsAtUtc, ApplicationOpensAtUtc,
                         ApplicationClosesAtUtc, Price, Currency, Capacity, Status, CreatedAtUtc)
                    SELECT Id, ProgramId, StartsAt, EndsAt,
                           DATEADD(day, -30, StartsAt), DATEADD(day, -1, StartsAt),
                           Price, Currency, Capacity,
                           CASE WHEN Status IN ('Draft', 'Open', 'Closed', 'Cancelled')
                                THEN Status ELSE 'Closed' END,
                           SYSDATETIMEOFFSET()
                    FROM [__LegacyProgramOfferingsImport];

                    DROP TABLE [__LegacyProgramOfferingsImport];
                END;
                """);

            // Preserve any local development applications/enrollments created before offerings existed.
            // Each referenced catalog item receives one closed legacy offering, and both tables are
            // repointed to it before the new foreign keys are applied.
            migrationBuilder.Sql(
                """
                DECLARE @CourseOfferingMap TABLE (CourseId uniqueidentifier PRIMARY KEY, OfferingId uniqueidentifier NOT NULL);

                INSERT INTO @CourseOfferingMap (CourseId, OfferingId)
                SELECT CourseId, NEWID()
                FROM (
                    SELECT CourseOfferingId AS CourseId FROM LearnerApplications WHERE CourseOfferingId IS NOT NULL
                    UNION
                    SELECT CourseOfferingId AS CourseId FROM Enrollments WHERE CourseOfferingId IS NOT NULL
                ) AS ReferencedCourses;

                INSERT INTO CourseOfferings
                    (Id, CourseId, StartsAtUtc, EndsAtUtc, ApplicationOpensAtUtc, ApplicationClosesAtUtc,
                     Price, Currency, Capacity, Status, CreatedAtUtc)
                SELECT map.OfferingId, map.CourseId,
                       '2000-03-01T00:00:00+00:00', '2000-04-01T00:00:00+00:00',
                       '2000-01-01T00:00:00+00:00', '2000-02-01T00:00:00+00:00',
                       COALESCE(course.Price, 0), 'EGP', 1, 'Closed', SYSDATETIMEOFFSET()
                FROM @CourseOfferingMap AS map
                INNER JOIN Courses AS course ON course.Id = map.CourseId;

                UPDATE application
                SET CourseOfferingId = map.OfferingId
                FROM LearnerApplications AS application
                INNER JOIN @CourseOfferingMap AS map ON map.CourseId = application.CourseOfferingId;

                UPDATE enrollment
                SET CourseOfferingId = map.OfferingId
                FROM Enrollments AS enrollment
                INNER JOIN @CourseOfferingMap AS map ON map.CourseId = enrollment.CourseOfferingId;

                DECLARE @ProgramOfferingMap TABLE (ProgramId uniqueidentifier PRIMARY KEY, OfferingId uniqueidentifier NOT NULL);

                INSERT INTO @ProgramOfferingMap (ProgramId, OfferingId)
                SELECT ProgramId, NEWID()
                FROM (
                    SELECT ProgramOfferingId AS ProgramId FROM LearnerApplications WHERE ProgramOfferingId IS NOT NULL
                    UNION
                    SELECT ProgramOfferingId AS ProgramId FROM Enrollments WHERE ProgramOfferingId IS NOT NULL
                ) AS ReferencedPrograms;

                INSERT INTO ProgramOfferings
                    (Id, ProgramId, StartsAtUtc, EndsAtUtc, ApplicationOpensAtUtc, ApplicationClosesAtUtc,
                     Price, Currency, Capacity, Status, CreatedAtUtc)
                SELECT map.OfferingId, map.ProgramId,
                       '2000-03-01T00:00:00+00:00', '2000-04-01T00:00:00+00:00',
                       '2000-01-01T00:00:00+00:00', '2000-02-01T00:00:00+00:00',
                       COALESCE(program.Price, 0), 'EGP', 1, 'Closed', SYSDATETIMEOFFSET()
                FROM @ProgramOfferingMap AS map
                INNER JOIN Programs AS program ON program.Id = map.ProgramId;

                UPDATE application
                SET ProgramOfferingId = map.OfferingId
                FROM LearnerApplications AS application
                INNER JOIN @ProgramOfferingMap AS map ON map.ProgramId = application.ProgramOfferingId;

                UPDATE enrollment
                SET ProgramOfferingId = map.OfferingId
                FROM Enrollments AS enrollment
                INNER JOIN @ProgramOfferingMap AS map ON map.ProgramId = enrollment.ProgramOfferingId;
                """);

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearnerApplications_ExactlyOneTarget",
                table: "LearnerApplications",
                sql: "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Enrollments_ExactlyOneOffering",
                table: "Enrollments",
                sql: "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferings_CourseId_StartsAtUtc",
                table: "CourseOfferings",
                columns: new[] { "CourseId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferings_CourseId_Status_StartsAtUtc",
                table: "CourseOfferings",
                columns: new[] { "CourseId", "Status", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOfferings_ProgramId_StartsAtUtc",
                table: "ProgramOfferings",
                columns: new[] { "ProgramId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOfferings_ProgramId_Status_StartsAtUtc",
                table: "ProgramOfferings",
                columns: new[] { "ProgramId", "Status", "StartsAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_CourseOfferings_CourseOfferingId",
                table: "Enrollments",
                column: "CourseOfferingId",
                principalTable: "CourseOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_ProgramOfferings_ProgramOfferingId",
                table: "Enrollments",
                column: "ProgramOfferingId",
                principalTable: "ProgramOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerApplications_CourseOfferings_CourseOfferingId",
                table: "LearnerApplications",
                column: "CourseOfferingId",
                principalTable: "CourseOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerApplications_ProgramOfferings_ProgramOfferingId",
                table: "LearnerApplications",
                column: "ProgramOfferingId",
                principalTable: "ProgramOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_CourseOfferings_CourseOfferingId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_ProgramOfferings_ProgramOfferingId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerApplications_CourseOfferings_CourseOfferingId",
                table: "LearnerApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerApplications_ProgramOfferings_ProgramOfferingId",
                table: "LearnerApplications");

            migrationBuilder.Sql(
                """
                UPDATE application
                SET CourseOfferingId = offering.CourseId
                FROM LearnerApplications AS application
                INNER JOIN CourseOfferings AS offering ON offering.Id = application.CourseOfferingId;

                UPDATE enrollment
                SET CourseOfferingId = offering.CourseId
                FROM Enrollments AS enrollment
                INNER JOIN CourseOfferings AS offering ON offering.Id = enrollment.CourseOfferingId;

                UPDATE application
                SET ProgramOfferingId = offering.ProgramId
                FROM LearnerApplications AS application
                INNER JOIN ProgramOfferings AS offering ON offering.Id = application.ProgramOfferingId;

                UPDATE enrollment
                SET ProgramOfferingId = offering.ProgramId
                FROM Enrollments AS enrollment
                INNER JOIN ProgramOfferings AS offering ON offering.Id = enrollment.ProgramOfferingId;
                """);

            migrationBuilder.DropTable(
                name: "CourseOfferings");

            migrationBuilder.DropTable(
                name: "ProgramOfferings");

            migrationBuilder.DropIndex(
                name: "IX_LearnerApplications_User_CourseOffering_Pending",
                table: "LearnerApplications");

            migrationBuilder.DropIndex(
                name: "IX_LearnerApplications_User_ProgramOffering_Pending",
                table: "LearnerApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearnerApplications_ExactlyOneTarget",
                table: "LearnerApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Enrollments_ExactlyOneOffering",
                table: "Enrollments");

            migrationBuilder.RenameColumn(
                name: "ProgramOfferingId",
                table: "LearnerApplications",
                newName: "ProgramId");

            migrationBuilder.RenameColumn(
                name: "CourseOfferingId",
                table: "LearnerApplications",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerApplications_ProgramOfferingId",
                table: "LearnerApplications",
                newName: "IX_LearnerApplications_ProgramId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerApplications_CourseOfferingId",
                table: "LearnerApplications",
                newName: "IX_LearnerApplications_CourseId");

            migrationBuilder.RenameColumn(
                name: "ProgramOfferingId",
                table: "Enrollments",
                newName: "ProgramId");

            migrationBuilder.RenameColumn(
                name: "CourseOfferingId",
                table: "Enrollments",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_ProgramOfferingId",
                table: "Enrollments",
                newName: "IX_Enrollments_ProgramId");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_CourseOfferingId",
                table: "Enrollments",
                newName: "IX_Enrollments_CourseId");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearnerApplications_ExactlyOneTarget",
                table: "LearnerApplications",
                sql: "([CourseId] IS NOT NULL AND [ProgramId] IS NULL) OR ([CourseId] IS NULL AND [ProgramId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Courses_CourseId",
                table: "Enrollments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Programs_ProgramId",
                table: "Enrollments",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerApplications_Courses_CourseId",
                table: "LearnerApplications",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerApplications_Programs_ProgramId",
                table: "LearnerApplications",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
