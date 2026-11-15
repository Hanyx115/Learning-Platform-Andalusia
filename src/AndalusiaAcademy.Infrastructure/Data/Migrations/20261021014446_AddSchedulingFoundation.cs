using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A discarded local feature branch created TrainingLocations in some developer databases
            // without adding it to EF's migration history. Adopt that table when present so existing
            // location data is preserved; create the same schema on clean databases.
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[TrainingLocations]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [TrainingLocations]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [BranchName] nvarchar(150) NOT NULL,
                        [RoomName] nvarchar(100) NOT NULL,
                        [City] nvarchar(100) NOT NULL,
                        [Address] nvarchar(500) NOT NULL,
                        [MapUrl] nvarchar(500) NULL,
                        [IsActive] bit NOT NULL,
                        CONSTRAINT [PK_TrainingLocations] PRIMARY KEY ([Id])
                    );

                    EXEC sys.sp_addextendedproperty
                        @name=N'CreatedByAddSchedulingFoundation', @value=1,
                        @level0type=N'SCHEMA', @level0name=N'dbo',
                        @level1type=N'TABLE', @level1name=N'TrainingLocations';
                END
                ELSE IF COL_LENGTH(N'TrainingLocations', N'MapUrl') IS NULL
                BEGIN
                    ALTER TABLE [TrainingLocations] ADD [MapUrl] nvarchar(500) NULL;

                    EXEC sys.sp_addextendedproperty
                        @name=N'AddedByAddSchedulingFoundation', @value=1,
                        @level0type=N'SCHEMA', @level0name=N'dbo',
                        @level1type=N'TABLE', @level1name=N'TrainingLocations',
                        @level2type=N'COLUMN', @level2name=N'MapUrl';
                END;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM [TrainingLocations]
                    WHERE [BranchName] = N'Alexandria Campus'
                      AND [RoomName] = N'Training Room A'
                      AND [City] = N'Alexandria')
                BEGIN
                    INSERT INTO [TrainingLocations]
                        ([Id], [BranchName], [RoomName], [City], [Address], [MapUrl], [IsActive])
                    VALUES
                        ('50000000-0000-0000-0000-000000000001', N'Alexandria Campus',
                         N'Training Room A', N'Alexandria',
                         N'39 Khalil El-Khayat Basha, Sidi Gaber', NULL, 1);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM [TrainingLocations]
                    WHERE [BranchName] = N'Alexandria Campus'
                      AND [RoomName] = N'Technology Lab'
                      AND [City] = N'Alexandria')
                BEGIN
                    INSERT INTO [TrainingLocations]
                        ([Id], [BranchName], [RoomName], [City], [Address], [MapUrl], [IsActive])
                    VALUES
                        ('50000000-0000-0000-0000-000000000002', N'Alexandria Campus',
                         N'Technology Lab', N'Alexandria',
                         N'39 Khalil El-Khayat Basha, Sidi Gaber', NULL, 1);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM [TrainingLocations]
                    WHERE [BranchName] = N'Online Learning'
                      AND [RoomName] = N'Virtual Classroom'
                      AND [City] = N'Online')
                BEGIN
                    INSERT INTO [TrainingLocations]
                        ([Id], [BranchName], [RoomName], [City], [Address], [MapUrl], [IsActive])
                    VALUES
                        ('50000000-0000-0000-0000-000000000003', N'Online Learning',
                         N'Virtual Classroom', N'Online', N'Live online delivery', NULL, 1);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM [TrainingLocations]
                    WHERE [BranchName] = N'Cairo Training Center'
                      AND [RoomName] = N'Training Room B'
                      AND [City] = N'Cairo')
                BEGIN
                    INSERT INTO [TrainingLocations]
                        ([Id], [BranchName], [RoomName], [City], [Address], [MapUrl], [IsActive])
                    VALUES
                        ('50000000-0000-0000-0000-000000000004', N'Cairo Training Center',
                         N'Training Room B', N'Cairo', N'Central Cairo training center', NULL, 1);
                END;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "ProgramOfferings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "CourseOfferings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE offering
                SET [LocationId] = COALESCE(
                    CASE course.[Type]
                        WHEN N'Online' THEN (
                            SELECT TOP (1) [Id] FROM [TrainingLocations]
                            WHERE [City] = N'Online' AND [IsActive] = 1 ORDER BY [Id])
                        WHEN N'Hybrid' THEN (
                            SELECT TOP (1) [Id] FROM [TrainingLocations]
                            WHERE [RoomName] = N'Technology Lab' AND [IsActive] = 1 ORDER BY [Id])
                        ELSE (
                            SELECT TOP (1) [Id] FROM [TrainingLocations]
                            WHERE [RoomName] = N'Training Room A' AND [IsActive] = 1 ORDER BY [Id])
                    END,
                    (SELECT TOP (1) [Id] FROM [TrainingLocations]
                     WHERE [IsActive] = 1 ORDER BY [Id]),
                    (SELECT TOP (1) [Id] FROM [TrainingLocations] ORDER BY [Id]))
                FROM [CourseOfferings] AS offering
                INNER JOIN [Courses] AS course ON course.[Id] = offering.[CourseId]
                WHERE offering.[LocationId] IS NULL;

                UPDATE offering
                SET [LocationId] = COALESCE(
                    (SELECT TOP (1) [Id] FROM [TrainingLocations]
                     WHERE [RoomName] = N'Training Room A' AND [IsActive] = 1 ORDER BY [Id]),
                    (SELECT TOP (1) [Id] FROM [TrainingLocations]
                     WHERE [IsActive] = 1 ORDER BY [Id]),
                    (SELECT TOP (1) [Id] FROM [TrainingLocations] ORDER BY [Id]))
                FROM [ProgramOfferings] AS offering
                WHERE offering.[LocationId] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "ProgramOfferings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "CourseOfferings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CourseSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstructorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessions", x => x.Id);
                    table.CheckConstraint("CK_CourseSessions_Cancellation", "([Status] = 'Cancelled' AND [CancellationReason] IS NOT NULL) OR ([Status] <> 'Cancelled' AND [CancellationReason] IS NULL)");
                    table.CheckConstraint("CK_CourseSessions_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
                    table.CheckConstraint("CK_CourseSessions_Status", "[Status] IN ('Scheduled', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_CourseSessions_AspNetUsers_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSessions_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSessions_TrainingLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TrainingLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOfferings_LocationId_Status_StartsAtUtc",
                table: "ProgramOfferings",
                columns: new[] { "LocationId", "Status", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferings_LocationId_Status_StartsAtUtc",
                table: "CourseOfferings",
                columns: new[] { "LocationId", "Status", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseOfferingId_StartsAtUtc",
                table: "CourseSessions",
                columns: new[] { "CourseOfferingId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_InstructorUserId_StartsAtUtc_EndsAtUtc",
                table: "CourseSessions",
                columns: new[] { "InstructorUserId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_LocationId_StartsAtUtc_EndsAtUtc",
                table: "CourseSessions",
                columns: new[] { "LocationId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_Status_StartsAtUtc",
                table: "CourseSessions",
                columns: new[] { "Status", "StartsAtUtc" });

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [object_id] = OBJECT_ID(N'[TrainingLocations]')
                      AND [name] = N'IX_TrainingLocations_BranchName_RoomName_City')
                BEGIN
                    CREATE UNIQUE INDEX [IX_TrainingLocations_BranchName_RoomName_City]
                        ON [TrainingLocations] ([BranchName], [RoomName], [City]);

                    EXEC sys.sp_addextendedproperty
                        @name=N'CreatedByAddSchedulingFoundation', @value=1,
                        @level0type=N'SCHEMA', @level0name=N'dbo',
                        @level1type=N'TABLE', @level1name=N'TrainingLocations',
                        @level2type=N'INDEX', @level2name=N'IX_TrainingLocations_BranchName_RoomName_City';
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [object_id] = OBJECT_ID(N'[TrainingLocations]')
                      AND [name] = N'IX_TrainingLocations_City_IsActive')
                BEGIN
                    CREATE INDEX [IX_TrainingLocations_City_IsActive]
                        ON [TrainingLocations] ([City], [IsActive]);

                    EXEC sys.sp_addextendedproperty
                        @name=N'CreatedByAddSchedulingFoundation', @value=1,
                        @level0type=N'SCHEMA', @level0name=N'dbo',
                        @level1type=N'TABLE', @level1name=N'TrainingLocations',
                        @level2type=N'INDEX', @level2name=N'IX_TrainingLocations_City_IsActive';
                END;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferings_TrainingLocations_LocationId",
                table: "CourseOfferings",
                column: "LocationId",
                principalTable: "TrainingLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramOfferings_TrainingLocations_LocationId",
                table: "ProgramOfferings",
                column: "LocationId",
                principalTable: "TrainingLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferings_TrainingLocations_LocationId",
                table: "CourseOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramOfferings_TrainingLocations_LocationId",
                table: "ProgramOfferings");

            migrationBuilder.DropTable(
                name: "CourseSessions");

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.fn_listextendedproperty(
                        N'CreatedByAddSchedulingFoundation',
                        N'SCHEMA', N'dbo', N'TABLE', N'TrainingLocations', NULL, NULL))
                BEGIN
                    DROP TABLE [TrainingLocations];
                END
                ELSE IF OBJECT_ID(N'[TrainingLocations]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM sys.fn_listextendedproperty(
                            N'CreatedByAddSchedulingFoundation',
                            N'SCHEMA', N'dbo', N'TABLE', N'TrainingLocations',
                            N'INDEX', N'IX_TrainingLocations_BranchName_RoomName_City'))
                    BEGIN
                        DROP INDEX [IX_TrainingLocations_BranchName_RoomName_City]
                            ON [TrainingLocations];
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.fn_listextendedproperty(
                            N'CreatedByAddSchedulingFoundation',
                            N'SCHEMA', N'dbo', N'TABLE', N'TrainingLocations',
                            N'INDEX', N'IX_TrainingLocations_City_IsActive'))
                    BEGIN
                        DROP INDEX [IX_TrainingLocations_City_IsActive]
                            ON [TrainingLocations];
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.fn_listextendedproperty(
                            N'AddedByAddSchedulingFoundation',
                            N'SCHEMA', N'dbo', N'TABLE', N'TrainingLocations',
                            N'COLUMN', N'MapUrl'))
                    BEGIN
                        ALTER TABLE [TrainingLocations] DROP COLUMN [MapUrl];
                    END;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ProgramOfferings_LocationId_Status_StartsAtUtc",
                table: "ProgramOfferings");

            migrationBuilder.DropIndex(
                name: "IX_CourseOfferings_LocationId_Status_StartsAtUtc",
                table: "CourseOfferings");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "ProgramOfferings");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "CourseOfferings");
        }
    }
}
