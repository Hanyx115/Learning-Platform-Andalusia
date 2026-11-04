using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndalusiaAcademy.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261104224500_AddLearnerPetsAndCatalogImages")]
public partial class AddLearnerPetsAndCatalogImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImageUrl",
            table: "Programs",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImageUrl",
            table: "Courses",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImageUrl",
            table: "CareerPaths",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "LearnerPetEnabled",
            table: "AspNetUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "LearnerPetType",
            table: "AspNetUsers",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "cat");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ImageUrl", table: "Programs");
        migrationBuilder.DropColumn(name: "ImageUrl", table: "Courses");
        migrationBuilder.DropColumn(name: "ImageUrl", table: "CareerPaths");
        migrationBuilder.DropColumn(name: "LearnerPetEnabled", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "LearnerPetType", table: "AspNetUsers");
    }
}
