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

public sealed class ApplicationsEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Submit_ForACourse_CreatesPendingApplication()
    {
        var (accessToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseId });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Pending", root.GetProperty("status").GetString());
        Assert.Equal(courseId.ToString(), root.GetProperty("courseId").GetString());
    }

    [Fact]
    public async Task Submit_WithBothCourseAndProgram_ReturnsValidationProblem()
    {
        var (accessToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();
        var programId = await SeedProgramAsync();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", accessToken, new { courseId, programId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_Twice_ForSameCourse_ReturnsValidationProblem()
    {
        var (accessToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();

        using var first = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseId });
        first.EnsureSuccessStatusCode();

        using var second = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseId });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnApplications()
    {
        var (accessToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseId });
        submitResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/applications/me", accessToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetPage_AsLearner_ReturnsForbidden()
    {
        var (accessToken, _) = await RegisterAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/applications", accessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accept_AsAdmin_TransitionsApplicationToAccepted()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetString();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);
        acceptResponse.EnsureSuccessStatusCode();

        using var acceptDocument = JsonDocument.Parse(await acceptResponse.Content.ReadAsStringAsync());
        Assert.Equal("Accepted", acceptDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Reject_ThenAcceptAgain_ReturnsNotFoundSinceNoLongerPending()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetString();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        using var rejectResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/reject", adminToken, new { reason = "Not eligible yet." });
        rejectResponse.EnsureSuccessStatusCode();

        using var secondAcceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);

        Assert.Equal(HttpStatusCode.NotFound, secondAcceptResponse.StatusCode);
    }

    [Fact]
    public async Task Accept_AsLearner_ReturnsForbidden()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseId = await SeedCourseAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetString();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", learnerToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_AsAdminWithoutLearnerRole_ReturnsForbidden()
    {
        var adminOnlyToken = await RegisterAdminOnlyAsync();
        var courseId = await SeedCourseAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", adminOnlyToken, new { courseId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private async Task<Guid> SeedCourseAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Cat-{Guid.NewGuid()}", Slug = $"cat-{Guid.NewGuid()}" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Course-{Guid.NewGuid()}",
            Slug = $"course-{Guid.NewGuid()}",
            Description = "Test course.",
            Status = "Published",
            Type = "Online",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.CourseCategories.Add(category);
        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        return course.Id;
    }

    private async Task<Guid> SeedProgramAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var program = new AcademyProgram
        {
            Id = Guid.NewGuid(),
            Title = $"Program-{Guid.NewGuid()}",
            Slug = $"program-{Guid.NewGuid()}",
            Description = "Test program.",
            Status = "Published",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Programs.Add(program);
        await dbContext.SaveChangesAsync();

        return program.Id;
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "P@ssw0rd123!",
            firstName = "Test",
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
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "Staff",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.AddToRoleAsync(user!, role);
        }

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> RegisterAdminOnlyAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "Staff",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.AddToRoleAsync(user!, RoleNames.Admin);
            await userManager.RemoveFromRoleAsync(user!, RoleNames.Learner);
        }

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }
}
