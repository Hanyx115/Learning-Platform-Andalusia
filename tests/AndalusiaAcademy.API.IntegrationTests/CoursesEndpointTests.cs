using System.Net;
using System.Text.Json;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CoursesEndpointTests(CatalogApiWebApplicationFactory factory)
    : IClassFixture<CatalogApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetCourses_ReturnsFilteredPaginatedResponse()
    {
        await factory.SeedCoursesAsync();

        using var response = await _client.GetAsync(
            "/api/v1/courses?search=Care&category=clinical-care" +
            "&status=Published&type=Offline&sort=title&page=1&pageSize=1");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Advanced Patient Care", root.GetProperty("items")[0].GetProperty("title").GetString());
        Assert.Equal("clinical-care", root.GetProperty("items")[0].GetProperty("categorySlug").GetString());
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task GetCourseOfferings_ReturnsOnlyCurrentlyOpenIntakes()
    {
        await factory.SeedCoursesAsync();
        var openOfferingId = await factory.SeedCourseOfferingsAsync();

        using var response = await _client.GetAsync("/api/v1/courses/patient-care-basics/offerings");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var offerings = document.RootElement;

        Assert.Equal(1, offerings.GetArrayLength());
        Assert.Equal(openOfferingId.ToString(), offerings[0].GetProperty("id").GetString());
        Assert.Equal(750m, offerings[0].GetProperty("price").GetDecimal());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("sort=unsupported")]
    public async Task GetCourses_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        using var response = await _client.GetAsync($"/api/v1/courses?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class CatalogApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CatalogApiTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = ApiWebApplicationFactory.TestJwtKey,
                ["CatalogSeed:Enabled"] = "false",
                ["Database:MigrateOnStartup"] = "false",
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    public async Task SeedCoursesAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await context.Courses.AnyAsync())
        {
            return;
        }

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
                "Published",
                "Offline",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateCourse(
                clinicalCare.Id,
                "Advanced Patient Care",
                "advanced-patient-care",
                "Published",
                "Offline",
                new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateCourse(
                management.Id,
                "Leadership Essentials",
                "leadership-essentials",
                "Published",
                "Online",
                new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)));

        await context.SaveChangesAsync();
    }

    public async Task SeedProgramsAndCareerPathsAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await context.Programs.AnyAsync() || await context.CareerPaths.AnyAsync())
        {
            return;
        }

        context.Programs.AddRange(
            CreateProgram("Alpha Program", "alpha-program", "Published", 1),
            CreateProgram("Beta Program", "beta-program", "Published", 2),
            CreateProgram("Gamma Program", "gamma-program", "Published", 3),
            CreateProgram("Internal Draft", "internal-draft", "Draft", 4));
        context.CareerPaths.AddRange(
            CreateCareerPath("Alpha Career", "alpha-career", "Published"),
            CreateCareerPath("Beta Career", "beta-career", "Published"),
            CreateCareerPath("Gamma Career", "gamma-career", "Published"),
            CreateCareerPath("Internal Career Draft", "internal-career-draft", "Draft"));

        await context.SaveChangesAsync();
    }

    public async Task<Guid> SeedCourseOfferingsAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var course = await context.Courses.SingleAsync(item => item.Slug == "patient-care-basics");
        var now = DateTimeOffset.UtcNow;
        var location = CreateLocation();
        context.TrainingLocations.Add(location);
        var openOffering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = now.AddMonths(2),
            EndsAtUtc = now.AddMonths(3),
            ApplicationOpensAtUtc = now.AddDays(-1),
            ApplicationClosesAtUtc = now.AddMonths(1),
            Price = 750m,
            Currency = "EGP",
            Capacity = 25,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = now,
        };
        var closedOffering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = now.AddMonths(4),
            EndsAtUtc = now.AddMonths(5),
            ApplicationOpensAtUtc = now.AddMonths(-2),
            ApplicationClosesAtUtc = now.AddMonths(-1),
            Price = 700m,
            Currency = "EGP",
            Capacity = 25,
            Status = OfferingStatuses.Closed,
            CreatedAtUtc = now,
        };

        context.CourseOfferings.AddRange(openOffering, closedOffering);
        await context.SaveChangesAsync();
        return openOffering.Id;
    }

    public async Task<Guid> SeedProgramOfferingAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var program = await context.Programs.SingleAsync(item => item.Slug == "alpha-program");
        var now = DateTimeOffset.UtcNow;
        var location = CreateLocation();
        context.TrainingLocations.Add(location);
        var offering = new ProgramOffering
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            LocationId = location.Id,
            StartsAtUtc = now.AddMonths(2),
            EndsAtUtc = now.AddMonths(6),
            ApplicationOpensAtUtc = now.AddDays(-1),
            ApplicationClosesAtUtc = now.AddMonths(1),
            Price = 3000m,
            Currency = "EGP",
            Capacity = 20,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = now,
        };

        context.ProgramOfferings.Add(offering);
        await context.SaveChangesAsync();
        return offering.Id;
    }

    private static TrainingLocation CreateLocation()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Test Branch {suffix}",
            RoomName = "Room 1",
            City = "Alexandria",
            Address = "Test address",
            IsActive = true,
        };
    }

    private static Course CreateCourse(
        Guid categoryId,
        string title,
        string slug,
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
            Description = $"Description for {title}",
            Status = status,
            Type = type,
            CreatedAt = createdAt,
        };
    }

    private static AcademyProgram CreateProgram(
        string title,
        string slug,
        string status,
        int month)
    {
        return new AcademyProgram
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Description = $"Description for {title}",
            Status = status,
            CreatedAt = new DateTimeOffset(2026, month, 1, 0, 0, 0, TimeSpan.Zero),
        };
    }

    private static CareerPath CreateCareerPath(string title, string slug, string status)
    {
        return new CareerPath
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Description = $"Description for {title}",
            RecommendedSkills = "Planning, communication",
            Status = status,
        };
    }
}
