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

public sealed class ProgressEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AssignedInstructor_SeesOfferingProgress_WithAttendanceAndAssignmentMetrics()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        await MarkPresentAsync(seed.SessionId, seed.LearnerId, seed.InstructorToken);
        await CreateSubmitAndGradeAssignmentAsync(seed, score: 80);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-offerings/{seed.OfferingId}/progress", seed.InstructorToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(seed.LearnerId.ToString(), record.GetProperty("userId").GetString());
        Assert.Equal(1, record.GetProperty("totalSessions").GetInt32());
        Assert.Equal(1, record.GetProperty("attendedSessions").GetInt32());
        Assert.Equal(100, record.GetProperty("attendanceRatePercentage").GetDecimal());
        Assert.Equal(1, record.GetProperty("totalAssignments").GetInt32());
        Assert.Equal(1, record.GetProperty("submittedAssignments").GetInt32());
        Assert.Equal(1, record.GetProperty("gradedAssignments").GetInt32());
        Assert.Equal(80, record.GetProperty("averageScorePercentage").GetDecimal());
        Assert.Equal(100, record.GetProperty("overallProgressPercentage").GetDecimal());
    }

    [Fact]
    public async Task UnassignedInstructor_CannotViewOfferingProgress()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        var otherInstructorToken = await RegisterAsAsync(RoleNames.Instructor);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-offerings/{seed.OfferingId}/progress", otherInstructorToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanViewOfferingProgress_RegardlessOfInstructorAssignment()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-offerings/{seed.OfferingId}/progress", adminToken);

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Single(document.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Learner_SeesOwnProgress_ReflectingAttendanceAndSubmissions()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        await MarkPresentAsync(seed.SessionId, seed.LearnerId, seed.InstructorToken);
        await CreateSubmitAndGradeAssignmentAsync(seed, score: 60);

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/learner/progress", seed.LearnerToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(seed.OfferingId.ToString(), record.GetProperty("courseOfferingId").GetString());
        Assert.Equal(100, record.GetProperty("attendanceRatePercentage").GetDecimal());
        Assert.Equal(1, record.GetProperty("submittedAssignments").GetInt32());
        Assert.Equal(60, record.GetProperty("averageScorePercentage").GetDecimal());
    }

    [Fact]
    public async Task Learner_WithNoAttendanceOrSubmissions_SeesZeroProgress()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/learner/progress", seed.LearnerToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(0, record.GetProperty("attendanceRatePercentage").GetDecimal());
        Assert.Equal(0, record.GetProperty("overallProgressPercentage").GetDecimal());
        Assert.Equal(JsonValueKind.Null, record.GetProperty("averageScorePercentage").ValueKind);
    }

    [Fact]
    public async Task Learner_MarkedLate_IsCountedAsAttendingTheSession()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        using var markResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{seed.SessionId}/attendance",
            seed.InstructorToken,
            new { entries = new[] { new { userId = seed.LearnerId, status = "Late", note = (string?)null } } });
        markResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/learner/progress", seed.LearnerToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(1, record.GetProperty("attendedSessions").GetInt32());
        Assert.Equal(100, record.GetProperty("attendanceRatePercentage").GetDecimal());
    }

    [Fact]
    public async Task SessionStillInProgress_IsNotCountedAsMissed()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await dbContext.CourseSessions.FindAsync(seed.SessionId);
            session!.StartsAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30);
            session.EndsAtUtc = DateTimeOffset.UtcNow.AddMinutes(30);
            session.Status = CourseSessionStatuses.Scheduled;
            await dbContext.SaveChangesAsync();
        }

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/learner/progress", seed.LearnerToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(0, record.GetProperty("totalSessions").GetInt32());
        Assert.Equal(0, record.GetProperty("attendanceRatePercentage").GetDecimal());
    }

    [Fact]
    public async Task FutureSession_AttendanceForAnotherLearner_DoesNotCountAgainstCurrentLearner()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        var (_, otherLearnerEmail) = await RegisterAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var otherLearner = (await userManager.FindByEmailAsync(otherLearnerEmail))!;
            var session = await dbContext.CourseSessions.FindAsync(seed.SessionId);
            session!.StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2);
            session.EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(2).AddHours(2);
            session.Status = CourseSessionStatuses.Scheduled;
            dbContext.CourseSessionAttendances.Add(new CourseSessionAttendance
            {
                Id = Guid.NewGuid(),
                CourseSessionId = seed.SessionId,
                UserId = otherLearner.Id,
                Status = AttendanceStatuses.Present,
                RecordedByUserId = otherLearner.Id,
                RecordedAtUtc = DateTimeOffset.UtcNow,
            });
            await dbContext.SaveChangesAsync();
        }

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/learner/progress", seed.LearnerToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(0, record.GetProperty("totalSessions").GetInt32());
        Assert.Equal(0, record.GetProperty("attendedSessions").GetInt32());
    }

    [Fact]
    public async Task FutureSession_WithRecordedAttendance_IsIncludedInProgress()
    {
        var seed = await SeedOfferingWithPastSessionAndEnrolledLearnerAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await dbContext.CourseSessions.FindAsync(seed.SessionId);
            session!.StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2);
            session.EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(2).AddHours(2);
            session.Status = CourseSessionStatuses.Scheduled;
            await dbContext.SaveChangesAsync();
        }

        await MarkPresentAsync(seed.SessionId, seed.LearnerId, seed.InstructorToken);

        using var learnerResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/learner/progress", seed.LearnerToken);
        learnerResponse.EnsureSuccessStatusCode();
        using var learnerDocument = JsonDocument.Parse(await learnerResponse.Content.ReadAsStringAsync());
        var learnerRecord = Assert.Single(learnerDocument.RootElement.EnumerateArray());
        Assert.Equal(1, learnerRecord.GetProperty("totalSessions").GetInt32());
        Assert.Equal(1, learnerRecord.GetProperty("attendedSessions").GetInt32());
        Assert.Equal(100, learnerRecord.GetProperty("attendanceRatePercentage").GetDecimal());

        using var offeringResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-offerings/{seed.OfferingId}/progress", seed.InstructorToken);
        offeringResponse.EnsureSuccessStatusCode();
        using var offeringDocument = JsonDocument.Parse(await offeringResponse.Content.ReadAsStringAsync());
        var offeringRecord = Assert.Single(offeringDocument.RootElement.EnumerateArray());
        Assert.Equal(1, offeringRecord.GetProperty("totalSessions").GetInt32());
        Assert.Equal(1, offeringRecord.GetProperty("attendedSessions").GetInt32());
    }

    private async Task MarkPresentAsync(Guid sessionId, Guid learnerId, string instructorToken)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = "Present", note = (string?)null } } });
        response.EnsureSuccessStatusCode();
    }

    private async Task CreateSubmitAndGradeAssignmentAsync(ProgressSeed seed, decimal score)
    {
        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/course-offerings/{seed.OfferingId}/assignments", seed.AdminToken,
            new
            {
                title = "Progress check essay",
                description = "Write a short reflection.",
                dueAtUtc = DateTimeOffset.UtcNow.AddDays(14),
                maxScore = 100,
            });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var assignmentId = createDocument.RootElement.GetProperty("id").GetGuid();

        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", seed.LearnerToken, new { content = "My reflection" });
        submitResponse.EnsureSuccessStatusCode();

        using var gradeResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/assignments/{assignmentId}/submissions/{seed.LearnerId}/grade", seed.InstructorToken,
            new { score, feedback = (string?)null });
        gradeResponse.EnsureSuccessStatusCode();
    }

    private sealed record ProgressSeed(
        Guid OfferingId, Guid SessionId, Guid LearnerId, string LearnerToken, string InstructorToken, string AdminToken);

    private async Task<ProgressSeed> SeedOfferingWithPastSessionAndEnrolledLearnerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Progress Cat {suffix}", Slug = $"progress-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Progress Course {suffix}",
            Slug = $"progress-course-{suffix}",
            Description = "desc",
            Status = "Published",
            Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Progress Branch {suffix}",
            RoomName = "Room 1",
            City = "Alexandria",
            Address = "Addr",
            Capacity = 100,
            IsActive = true,
        };
        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(2);
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = startsAtUtc.AddMonths(2),
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

        var (instructorToken, instructorId) = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        // Seeded directly: the API's session-creation endpoint requires sessions to start in the
        // future, but Progress needs an already-elapsed session to compute a meaningful attendance rate.
        var session = new CourseSession
        {
            Id = Guid.NewGuid(),
            CourseOfferingId = offering.Id,
            LocationId = location.Id,
            InstructorUserId = instructorId,
            Title = "Progress seed session",
            StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            EndsAtUtc = DateTimeOffset.UtcNow.AddDays(-7).AddHours(2),
            Status = CourseSessionStatuses.Completed,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

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

        return new ProgressSeed(offering.Id, session.Id, learnerId, learnerToken, instructorToken, adminToken);
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
            firstName = "Progress",
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
            firstName = "Progress",
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
