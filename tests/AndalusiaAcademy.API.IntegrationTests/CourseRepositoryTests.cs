using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Catalog.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CourseRepositoryTests
{
    [Fact]
    public async Task GetPageAsync_AppliesSearchAndFilters()
    {
        await using var context = CreateContext();
        await SeedCoursesAsync(context);
        var repository = new CourseRepository(context);
        var query = new CourseCatalogQuery
        {
            Search = "Care",
            Category = "clinical-care",
            Status = "Published",
            Type = "Offline",
            Sort = CourseCatalogQuery.TitleSort,
            Page = 1,
            PageSize = 10,
        };

        var result = await repository.GetPageAsync(query, CancellationToken.None);

        Assert.Equal(
            ["Advanced Patient Care", "Patient Care Basics"],
            result.Items.Select(course => course.Title).OrderBy(title => title));
        Assert.All(result.Items, course => Assert.Equal("Clinical Care", course.CategoryName));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsTheRequestedPageWithStableTitleOrdering()
    {
        await using var context = CreateContext();
        await SeedCoursesAsync(context);
        var repository = new CourseRepository(context);
        var query = new CourseCatalogQuery
        {
            Sort = CourseCatalogQuery.TitleSort,
            Page = 2,
            PageSize = 2,
        };

        var result = await repository.GetPageAsync(query, CancellationToken.None);

        Assert.Equal(["Leadership Essentials", "Patient Care Basics"], result.Items.Select(course => course.Title));
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedCoursesAsync(ApplicationDbContext context)
    {
        var clinicalCare = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = "Clinical Care",
            Slug = "clinical-care",
        };
        var management = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = "Management",
            Slug = "management",
        };

        context.CourseCategories.AddRange(clinicalCare, management);
        context.Courses.AddRange(
            CreateCourse(
                clinicalCare.Id,
                "Patient Care Basics",
                "patient-care-basics",
                "Essential care skills",
                "Published",
                "Offline",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateCourse(
                clinicalCare.Id,
                "Advanced Patient Care",
                "advanced-patient-care",
                "Advanced clinical practice",
                "Published",
                "Offline",
                new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateCourse(
                clinicalCare.Id,
                "Emergency Care",
                "emergency-care",
                "Emergency response",
                "Draft",
                "Offline",
                new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateCourse(
                management.Id,
                "Leadership Essentials",
                "leadership-essentials",
                "Healthcare leadership",
                "Published",
                "Online",
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));

        await context.SaveChangesAsync();
    }

    private static Course CreateCourse(
        Guid categoryId,
        string title,
        string slug,
        string description,
        string status,
        string type,
        DateTimeOffset createdAt)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Title = title,
            Slug = slug,
            Description = description,
            Status = status,
            Type = type,
            CreatedAt = createdAt,
        };
    }
}
