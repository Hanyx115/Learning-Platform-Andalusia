using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensationAndWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstructorCourseRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstructorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    SetByManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorCourseRates", x => x.Id);
                    table.CheckConstraint("CK_InstructorCourseRates_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_InstructorCourseRates_AspNetUsers_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstructorCourseRates_AspNetUsers_SetByManagerUserId",
                        column: x => x.SetByManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstructorCourseRates_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffCompensationRates",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    SetByAdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffCompensationRates", x => x.UserId);
                    table.CheckConstraint("CK_StaffCompensationRates_Amount", "[MonthlyAmount] > 0");
                    table.ForeignKey(
                        name: "FK_StaffCompensationRates_AspNetUsers_SetByAdminUserId",
                        column: x => x.SetByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffCompensationRates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletAccounts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAccounts", x => x.UserId);
                    table.CheckConstraint("CK_WalletAccounts_Balance", "[Balance] >= 0");
                    table.ForeignKey(
                        name: "FK_WalletAccounts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompensationPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InstructorCourseRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodMonth = table.Column<DateOnly>(type: "date", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedByManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByAdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationPayouts", x => x.Id);
                    table.CheckConstraint("CK_CompensationPayouts_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_CompensationPayouts_Status", "[Status] IN ('Draft', 'Reviewed', 'Paid')");
                    table.CheckConstraint("CK_CompensationPayouts_Type", "[PayoutType] IN ('InstructorCourse', 'StaffMonthly')");
                    table.ForeignKey(
                        name: "FK_CompensationPayouts_AspNetUsers_ApprovedByAdminUserId",
                        column: x => x.ApprovedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompensationPayouts_AspNetUsers_ReviewedByManagerUserId",
                        column: x => x.ReviewedByManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompensationPayouts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompensationPayouts_InstructorCourseRates_InstructorCourseRateId",
                        column: x => x.InstructorCourseRateId,
                        principalTable: "InstructorCourseRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompensationPayouts_ApprovedByAdminUserId",
                table: "CompensationPayouts",
                column: "ApprovedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationPayouts_InstructorCourseRateId",
                table: "CompensationPayouts",
                column: "InstructorCourseRateId",
                unique: true,
                filter: "[InstructorCourseRateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationPayouts_ReviewedByManagerUserId",
                table: "CompensationPayouts",
                column: "ReviewedByManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationPayouts_UserId_PeriodMonth_PayoutType",
                table: "CompensationPayouts",
                columns: new[] { "UserId", "PeriodMonth", "PayoutType" },
                unique: true,
                filter: "[PeriodMonth] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorCourseRates_CourseOfferingId",
                table: "InstructorCourseRates",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorCourseRates_InstructorUserId_CourseOfferingId",
                table: "InstructorCourseRates",
                columns: new[] { "InstructorUserId", "CourseOfferingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorCourseRates_SetByManagerUserId",
                table: "InstructorCourseRates",
                column: "SetByManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffCompensationRates_SetByAdminUserId",
                table: "StaffCompensationRates",
                column: "SetByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Reference",
                table: "WalletTransactions",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId_CreatedAtUtc",
                table: "WalletTransactions",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompensationPayouts");

            migrationBuilder.DropTable(
                name: "StaffCompensationRates");

            migrationBuilder.DropTable(
                name: "WalletAccounts");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "InstructorCourseRates");
        }
    }
}
