using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Catalog.Seeding;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CatalogDataSeederTests
{
    [Fact]
    public async Task SeedAsync_AddsCompleteCatalogAndRelationships()
    {
        await using var context = CreateContext();

        var addedRows = await CatalogDataSeeder.SeedAsync(context);

        Assert.Equal(111, addedRows);
        Assert.Equal(7, await context.CourseCategories.CountAsync());
        Assert.Equal(22, await context.Courses.CountAsync());
        Assert.Equal(9, await context.Programs.CountAsync());
        Assert.Equal(27, await context.ProgramCourses.CountAsync());
        Assert.Equal(10, await context.CareerPaths.CountAsync());
        Assert.Equal(10, await context.CareerPathPrograms.CountAsync());
        Assert.Equal(26, await context.CareerPathCourses.CountAsync());

        var emergencyProgram = await context.Programs
            .SingleAsync(program => program.Slug == "emergency-care-foundations");
        var orderedCourseSlugs = await context.ProgramCourses
            .Where(link => link.ProgramId == emergencyProgram.Id)
            .OrderBy(link => link.SequenceOrder)
            .Select(link => link.Course!.Slug)
            .ToListAsync();

        Assert.Equal(
            [
                "basic-life-support-essentials",
                "fundamentals-of-patient-assessment",
                "emergency-response-fundamentals",
            ],
            orderedCourseSlugs);

        var softwareCareerPath = await context.CareerPaths
            .SingleAsync(careerPath => careerPath.Slug == "software-engineer");
        var softwareCourseSlugs = await context.CareerPathCourses
            .Where(link => link.CareerPathId == softwareCareerPath.Id)
            .Select(link => link.Course!.Slug)
            .OrderBy(slug => slug)
            .ToListAsync();

        Assert.Equal(
            [
                "data-structures-and-algorithms",
                "object-oriented-programming-csharp",
                "programming-fundamentals",
            ],
            softwareCourseSlugs);

        var entrepreneurshipProgram = await context.Programs
            .SingleAsync(program => program.Slug == "entrepreneurship-launchpad");
        var entrepreneurshipCourseSlugs = await context.ProgramCourses
            .Where(link => link.ProgramId == entrepreneurshipProgram.Id)
            .OrderBy(link => link.SequenceOrder)
            .Select(link => link.Course!.Slug)
            .ToListAsync();

        Assert.Equal(
            [
                "entrepreneurship-business-planning",
                "financial-literacy-for-professionals",
                "digital-marketing-strategy",
            ],
            entrepreneurshipCourseSlugs);
    }

    [Fact]
    public async Task SeedAsync_WhenRunAgain_DoesNotDuplicateOrOverwriteCatalogData()
    {
        await using var context = CreateContext();
        var existingCategory = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = "Existing Clinical Category",
            Slug = "clinical-care",
        };
        context.CourseCategories.Add(existingCategory);
        await context.SaveChangesAsync();

        var firstAddedRows = await CatalogDataSeeder.SeedAsync(context);
        var secondAddedRows = await CatalogDataSeeder.SeedAsync(context);

        Assert.Equal(110, firstAddedRows);
        Assert.Equal(0, secondAddedRows);
        Assert.Equal(7, await context.CourseCategories.CountAsync());
        Assert.Equal(22, await context.Courses.CountAsync());
        Assert.Equal(9, await context.Programs.CountAsync());
        Assert.Equal(10, await context.CareerPaths.CountAsync());

        var preservedCategory = await context.CourseCategories
            .SingleAsync(category => category.Slug == "clinical-care");
        Assert.Equal(existingCategory.Id, preservedCategory.Id);
        Assert.Equal("Existing Clinical Category", preservedCategory.Name);

        var clinicalCourseCategoryIds = await context.Courses
            .Where(course => course.Slug == "clinical-communication-skills" ||
                             course.Slug == "fundamentals-of-patient-assessment")
            .Select(course => course.CategoryId)
            .Distinct()
            .ToListAsync();
        Assert.Equal([existingCategory.Id], clinicalCourseCategoryIds);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
