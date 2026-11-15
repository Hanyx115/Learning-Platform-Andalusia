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

public sealed class InstructorDashboardEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Instructor_SeesOwnSessionsOfferingsAndStudents()
    {
        var seed = await SeedOfferingSessionAndEnrolledLearnerAsync();

        using var sessionsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/instructor/sessions", seed.InstructorToken);
        sessionsResponse.EnsureSuccessStatusCode();
        using var sessionsDocument = JsonDocument.Parse(await sessionsResponse.Content.ReadAsStringAsync());
        var sessionEntry = Assert.Single(sessionsDocument.RootElement.EnumerateArray());
        Assert.Equal(seed.SessionId.ToString(), sessionEntry.GetProperty("sessionId").GetString());
        Assert.Equal(seed.OfferingId.ToString(), sessionEntry.GetProperty("courseOfferingId").GetString());

        using var offeringsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/instructor/offerings", seed.InstructorToken);
        offeringsResponse.EnsureSuccessStatusCode();
        using var offeringsDocument = JsonDocument.Parse(await offeringsResponse.Content.ReadAsStringAsync());
        var offeringEntry = Assert.Single(offeringsDocument.RootElement.EnumerateArray());
        Assert.Equal(seed.OfferingId.ToString(), offeringEntry.GetProperty("courseOfferingId").GetString());
        Assert.Equal(1, offeringEntry.GetProperty("enrolledStudentCount").GetInt32());

        using var studentsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/instructor/students", seed.InstructorToken);
        studentsResponse.EnsureSuccessStatusCode();
        using var studentsDocument = JsonDocument.Parse(await studentsResponse.Content.ReadAsStringAsync());
        var studentEntry = Assert.Single(studentsDocument.RootElement.EnumerateArray());
        Assert.Equal(seed.LearnerId.ToString(), studentEntry.GetProperty("userId").GetString());

        using var overviewResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/instructor/overview", seed.InstructorToken);
        overviewResponse.EnsureSuccessStatusCode();
        using var overviewDocument = JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, overviewDocument.RootElement.GetProperty("assignedOfferings").GetInt32());
        Assert.Equal(1, overviewDocument.RootElement.GetProperty("activeStudents").GetInt32());
        Assert.Equal(1, overviewDocument.RootElement.GetProperty("upcomingSessions").GetInt32());
        Assert.Equal(0, overviewDocument.RootElement.GetProperty("pendingSubmissions").GetInt32());
        Assert.Single(overviewDocument.RootElement.GetProperty("nextSessions").EnumerateArray());
    }

    [Fact]
    public async Task UnassignedInstructor_SeesEmptyDashboard()
    {
        await SeedOfferingSessionAndEnrolledLearnerAsync();
        var otherInstructorToken = await RegisterAsAsync(RoleNames.Instructor);

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/instructor/sessions", otherInstructorToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(document.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Learner_CannotAccessInstructorDashboard()
    {
        var (learnerToken, _) = await RegisterAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/instructor/sessions", learnerToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record DashboardSeed(
        Guid OfferingId, Guid SessionId, Guid LearnerId, string InstructorToken);

    private async Task<DashboardSeed> SeedOfferingSessionAndEnrolledLearnerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Instructor Cat {suffix}", Slug = $"instructor-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Instructor Course {suffix}",
            Slug = $"instructor-course-{suffix}", Description = "desc", Status = "Published", Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(), BranchName = $"Instructor Branch {suffix}", RoomName = "Room 1",
            City = "Alexandria", Address = "Addr", Capacity = 100, IsActive = true,
        };
        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(2);
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(), CourseId = course.Id, LocationId = location.Id,
            StartsAtUtc = startsAtUtc, EndsAtUtc = startsAtUtc.AddMonths(2),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = 500m, Currency = "EGP", Capacity = 20, Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.AddRange(category, course, location, offering);
        await dbContext.SaveChangesAsync();

        var (instructorToken, instructorId) = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        var sessionStartsAtUtc = offering.StartsAtUtc.AddDays(1);
        using var createSessionResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{offering.Id}/sessions",
            adminToken,
            new
            {
                locationId = location.Id,
                instructorUserId = instructorId,
                title = "Instructor dashboard seed session",
                startsAtUtc = sessionStartsAtUtc,
                endsAtUtc = sessionStartsAtUtc.AddHours(2),
            });
        createSessionResponse.EnsureSuccessStatusCode();
        using var sessionDocument = JsonDocument.Parse(await createSessionResponse.Content.ReadAsStringAsync());
        var sessionId = sessionDocument.RootElement.GetProperty("id").GetGuid();

        var (learnerToken, learnerEmail) = await RegisterAsync();

        using var applyResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId = offering.Id });
        applyResponse.EnsureSuccessStatusCode();
        using var applyDocument = JsonDocument.Parse(await applyResponse.Content.ReadAsStringAsync());
        var applicationId = applyDocument.RootElement.GetProperty("id").GetGuid();

        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);
        acceptResponse.EnsureSuccessStatusCode();

        using var payResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });
        payResponse.EnsureSuccessStatusCode();

        Guid learnerId;
        using (var userScope = factory.Services.CreateScope())
        {
            var userManager = userScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            learnerId = (await userManager.FindByEmailAsync(learnerEmail))!.Id;
        }

        return new DashboardSeed(offering.Id, sessionId, learnerId, instructorToken);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method, string url, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Instructor",
            lastName = "Learner",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (document.RootElement.GetProperty("accessToken").GetString()!, email);
    }

    private async Task<string> RegisterAsAsync(string role) => (await RegisterAsWithUserIdAsync(role)).AccessToken;

    private async Task<(string AccessToken, Guid UserId)> RegisterAsWithUserIdAsync(string role)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Instructor",
            lastName = "Staff",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByEmailAsync(email))!;
            await userManager.AddToRoleAsync(user, role);
            userId = user.Id;
        }

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return (document.RootElement.GetProperty("accessToken").GetString()!, userId);
    }
}
