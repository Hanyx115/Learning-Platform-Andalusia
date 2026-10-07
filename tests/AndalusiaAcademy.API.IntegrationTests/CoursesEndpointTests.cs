using System.Net;
using System.Text.Json;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
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
}
