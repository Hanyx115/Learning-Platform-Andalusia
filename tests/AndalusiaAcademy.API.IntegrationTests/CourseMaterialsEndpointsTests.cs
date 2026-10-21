using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CourseMaterialsEndpointsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Visitor_CannotAccessEnrollmentMaterials()
    {
        using var response = await _client.GetAsync(
            $"/api/v1/learner/enrollments/{Guid.NewGuid()}/materials");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CanCreateAndUpdateMaterial_WhileLearnerCannotManage()
    {
        var seeded = await SeedCourseOfferingAsync();
        var managerToken = await RegisterAsAsync(RoleNames.Manager);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/courses/{seeded.CourseId}/materials",
            managerToken,
            new
            {
                courseSessionId = (Guid?)null,
                title = "Networking workbook",
                description = "Reference exercises for the course.",
                resourceUrl = "https://learning.example.com/networking.pdf",
                materialType = CourseMaterialTypes.Document,
                sortOrder = 1,
                visibility = CourseMaterialVisibilities.Published,
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var materialId = createDocument.RootElement.GetProperty("id").GetGuid();

        using var updateResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-materials/{materialId}",
            managerToken,
            new
            {
                courseSessionId = (Guid?)null,
                title = "Networking workbook - revised",
                description = "Revised reference exercises.",
                resourceUrl = "https://learning.example.com/networking-v2.pdf",
                materialType = CourseMaterialTypes.Document,
                sortOrder = 2,
                visibility = CourseMaterialVisibilities.Hidden,
            });

        updateResponse.EnsureSuccessStatusCode();
        using var updateDocument = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Hidden", updateDocument.RootElement.GetProperty("visibility").GetString());

        var (learnerToken, _) = await RegisterAsync();
        using var forbiddenResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/courses/{seeded.CourseId}/materials",
            learnerToken,
            new
            {
                title = "Unauthorized material",
                resourceUrl = "https://learning.example.com/unauthorized.pdf",
                materialType = CourseMaterialTypes.Document,
                sortOrder = 0,
                visibility = CourseMaterialVisibilities.Published,
            });

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task EnrolledLearner_ReceivesPublishedMaterialsOnly_AndCannotReadAnotherEnrollment()
    {
        var (learnerToken, learnerEmail) = await RegisterAsync();
        var seeded = await SeedCourseOfferingAsync();
        var enrollmentId = await EnrollAsync(learnerToken, seeded.OfferingId);
        await SeedMaterialsAsync(seeded.CourseId, learnerEmail);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/learner/enrollments/{enrollmentId}/materials",
            learnerToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var material = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Published workbook", material.GetProperty("title").GetString());
        Assert.Equal("Published", material.GetProperty("visibility").GetString());

        var (otherLearnerToken, _) = await RegisterAsync();
        using var otherLearnerResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/learner/enrollments/{enrollmentId}/materials",
            otherLearnerToken);

        Assert.Equal(HttpStatusCode.NotFound, otherLearnerResponse.StatusCode);
    }

    [Fact]
    public async Task ProgramEnrollee_ReceivesPublishedMaterialsForProgramCourses()
    {
        var (learnerToken, learnerEmail) = await RegisterAsync();
        var seeded = await SeedProgramOfferingWithCourseAsync();
        var enrollmentId = await EnrollInProgramAsync(learnerToken, seeded.OfferingId);
        await SeedMaterialsAsync(seeded.CourseId, learnerEmail);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/learner/enrollments/{enrollmentId}/materials",
            learnerToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var material = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Published workbook", material.GetProperty("title").GetString());
    }

    private async Task<Guid> EnrollAsync(string learnerToken, Guid courseOfferingId)
    {
        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/applications",
            learnerToken,
            new { courseOfferingId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetGuid();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/applications/{applicationId}/accept",
            adminToken);
        acceptResponse.EnsureSuccessStatusCode();

        using var paymentResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/payments",
            learnerToken,
            new { applicationId });
        paymentResponse.EnsureSuccessStatusCode();

        using var paymentDocument = JsonDocument.Parse(await paymentResponse.Content.ReadAsStringAsync());
        return paymentDocument.RootElement.GetProperty("enrollment").GetProperty("id").GetGuid();
    }

    private async Task<Guid> EnrollInProgramAsync(string learnerToken, Guid programOfferingId)
    {
        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/applications",
            learnerToken,
            new { programOfferingId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetGuid();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/applications/{applicationId}/accept",
            adminToken);
        acceptResponse.EnsureSuccessStatusCode();

        using var paymentResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/payments",
            learnerToken,
            new { applicationId });
        paymentResponse.EnsureSuccessStatusCode();

        using var paymentDocument = JsonDocument.Parse(await paymentResponse.Content.ReadAsStringAsync());
        return paymentDocument.RootElement.GetProperty("enrollment").GetProperty("id").GetGuid();
    }

    private async Task SeedMaterialsAsync(Guid courseId, string createdByEmail)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var creator = await userManager.FindByEmailAsync(createdByEmail);

        dbContext.CourseMaterials.AddRange(
            new CourseMaterial
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Published workbook",
                ResourceUrl = "https://learning.example.com/published.pdf",
                MaterialType = CourseMaterialTypes.Document,
                SortOrder = 1,
                Visibility = CourseMaterialVisibilities.Published,
                CreatedByUserId = creator!.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            new CourseMaterial
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Hidden draft",
                ResourceUrl = "https://learning.example.com/draft.pdf",
                MaterialType = CourseMaterialTypes.Document,
                SortOrder = 2,
                Visibility = CourseMaterialVisibilities.Hidden,
                CreatedByUserId = creator.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });

        await dbContext.SaveChangesAsync();
    }

    private async Task<(Guid CourseId, Guid OfferingId)> SeedCourseOfferingAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = $"Materials Category {suffix}",
            Slug = $"materials-category-{suffix}",
        };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Materials Course {suffix}",
            Slug = $"materials-course-{suffix}",
            Description = "Course used to verify learning materials.",
            Status = "Published",
            Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Materials Branch {suffix}",
            RoomName = "Room 1",
            City = "Alexandria",
            Address = "Test materials address",
            IsActive = true,
        };
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
            EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(4),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = 500m,
            Currency = "EGP",
            Capacity = 20,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.AddRange(category, course, location, offering);
        await dbContext.SaveChangesAsync();
        return (course.Id, offering.Id);
    }

    private async Task<(Guid CourseId, Guid OfferingId)> SeedProgramOfferingWithCourseAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = $"Program Materials Category {suffix}",
            Slug = $"program-materials-category-{suffix}",
        };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Program Materials Course {suffix}",
            Slug = $"program-materials-course-{suffix}",
            Description = "Course included in the test program.",
            Status = "Published",
            Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var program = new AcademyProgram
        {
            Id = Guid.NewGuid(),
            Title = $"Materials Program {suffix}",
            Slug = $"materials-program-{suffix}",
            Description = "Program used to verify course-material access.",
            Status = "Published",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var programCourse = new ProgramCourse
        {
            ProgramId = program.Id,
            CourseId = course.Id,
            SequenceOrder = 1,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Program Materials Branch {suffix}",
            RoomName = "Room 2",
            City = "Alexandria",
            Address = "Test program materials address",
            IsActive = true,
        };
        var offering = new ProgramOffering
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            LocationId = location.Id,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
            EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(6),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = 2000m,
            Currency = "EGP",
            Capacity = 20,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.AddRange(category, course, program, programCourse, location, offering);
        await dbContext.SaveChangesAsync();
        return (course.Id, offering.Id);
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Materials",
            lastName = "Learner",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (document.RootElement.GetProperty("accessToken").GetString()!, email);
    }

    private async Task<string> RegisterAsAsync(string role)
    {
        var (initialToken, email) = await RegisterAsync();
        _ = initialToken;
        const string password = "P@ssw0rd123!";

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.AddToRoleAsync(user!, role);
        }

        using var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string url,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }
}
