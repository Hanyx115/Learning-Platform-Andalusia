using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsPassportAndLearningCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProgramOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityPosts", x => x.Id);
                    table.CheckConstraint("CK_CommunityPosts_ExactlyOneOffering", "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)");
                    table.CheckConstraint("CK_CommunityPosts_PostType", "[PostType] IN ('Discussion', 'Announcement')");
                    table.ForeignKey(
                        name: "FK_CommunityPosts_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityPosts_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityPosts_ProgramOfferings_ProgramOfferingId",
                        column: x => x.ProgramOfferingId,
                        principalTable: "ProgramOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReplies_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityReplies_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningSkillMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningSkillMappings", x => x.Id);
                    table.CheckConstraint("CK_LearningSkillMappings_ExactlyOneCatalogItem", "([CourseId] IS NOT NULL AND [ProgramId] IS NULL) OR ([CourseId] IS NULL AND [ProgramId] IS NOT NULL)");
                    table.CheckConstraint("CK_LearningSkillMappings_TargetLevel", "[TargetLevel] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_LearningSkillMappings_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningSkillMappings_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningSkillMappings_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_AuthorUserId",
                table: "CommunityPosts",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_CourseOfferingId_IsPinned_CreatedAtUtc",
                table: "CommunityPosts",
                columns: new[] { "CourseOfferingId", "IsPinned", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_ProgramOfferingId_IsPinned_CreatedAtUtc",
                table: "CommunityPosts",
                columns: new[] { "ProgramOfferingId", "IsPinned", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReplies_AuthorUserId",
                table: "CommunityReplies",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReplies_PostId_CreatedAtUtc",
                table: "CommunityReplies",
                columns: new[] { "PostId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningSkillMappings_CourseId",
                table: "LearningSkillMappings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSkillMappings_ProgramId",
                table: "LearningSkillMappings",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSkillMappings_SkillId_CourseId",
                table: "LearningSkillMappings",
                columns: new[] { "SkillId", "CourseId" },
                unique: true,
                filter: "[CourseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSkillMappings_SkillId_ProgramId",
                table: "LearningSkillMappings",
                columns: new[] { "SkillId", "ProgramId" },
                unique: true,
                filter: "[ProgramId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityReplies");

            migrationBuilder.DropTable(
                name: "LearningSkillMappings");

            migrationBuilder.DropTable(
                name: "CommunityPosts");

            migrationBuilder.DropTable(
                name: "Skills");
        }
    }
}
