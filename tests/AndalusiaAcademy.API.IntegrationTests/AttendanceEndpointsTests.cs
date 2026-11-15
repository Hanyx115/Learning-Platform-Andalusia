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

public sealed class AttendanceEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AssignedInstructor_CanViewRosterAndMarkAttendance()
    {
        var (sessionId, _, learnerId, _, instructorToken) = await SeedOfferingSessionAndEnrolledLearnerAsync();

        using var rosterResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-sessions/{sessionId}/attendance", instructorToken);
        rosterResponse.EnsureSuccessStatusCode();
        using var rosterDocument = JsonDocument.Parse(await rosterResponse.Content.ReadAsStringAsync());
        var rosterEntry = Assert.Single(rosterDocument.RootElement.EnumerateArray());
        Assert.Equal(learnerId.ToString(), rosterEntry.GetProperty("userId").GetString());
        Assert.Equal(JsonValueKind.Null, rosterEntry.GetProperty("status").ValueKind);

        using var markResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = "Present", note = (string?)null } } });
        markResponse.EnsureSuccessStatusCode();

        using var markedDocument = JsonDocument.Parse(await markResponse.Content.ReadAsStringAsync());
        var markedEntry = Assert.Single(markedDocument.RootElement.EnumerateArray());
        Assert.Equal("Present", markedEntry.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UnassignedInstructor_GetsNotFound()
    {
        var (sessionId, _, _, _, _) = await SeedOfferingSessionAndEnrolledLearnerAsync();
        var otherInstructorToken = await RegisterAsAsync(RoleNames.Instructor);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-sessions/{sessionId}/attendance", otherInstructorToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanMarkAttendance_RegardlessOfInstructorAssignment()
    {
        var (sessionId, _, learnerId, _, _) = await SeedOfferingSessionAndEnrolledLearnerAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            adminToken,
            new { entries = new[] { new { userId = learnerId, status = "Absent", note = "Called in sick" } } });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entry = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Absent", entry.GetProperty("status").GetString());
        Assert.Equal("Called in sick", entry.GetProperty("note").GetString());
    }

    [Fact]
    public async Task Learner_CannotAccessAttendanceEndpoints()
    {
        var (sessionId, _, _, _, _) = await SeedOfferingSessionAndEnrolledLearnerAsync();
        var (learnerToken, _) = await RegisterAsync();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-sessions/{sessionId}/attendance", learnerToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkAttendance_ForUnenrolledLearner_ReturnsValidationProblem()
    {
        var (sessionId, _, _, _, instructorToken) = await SeedOfferingSessionAndEnrolledLearnerAsync();
        var (_, strangerEmail) = await RegisterAsync();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stranger = await userManager.FindByEmailAsync(strangerEmail);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = stranger!.Id, status = "Present", note = (string?)null } } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkAttendance_ForCancelledSession_ReturnsConflict()
    {
        var (sessionId, _, learnerId, _, instructorToken) = await SeedOfferingSessionAndEnrolledLearnerAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await dbContext.CourseSessions.FindAsync(sessionId);
            session!.Status = CourseSessionStatuses.Cancelled;
            session.CancellationReason = "Session cancelled for testing.";
            await dbContext.SaveChangesAsync();
        }

        using var response = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = "Present", note = (string?)null } } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Learner_CanSeeOwnAttendanceAfterBeingMarked()
    {
        var (sessionId, _, learnerId, learnerToken, instructorToken) = await SeedOfferingSessionAndEnrolledLearnerAsync();

        using var markResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = "Late", note = (string?)null } } });
        markResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/learner/attendance", learnerToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Late", record.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ClearingAttendance_RemovesItFromTheLearnerAccount()
    {
        var (sessionId, _, learnerId, learnerToken, instructorToken) = await SeedOfferingSessionAndEnrolledLearnerAsync();

        using var markResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = "Present", note = "Recorded by mistake" } } });
        markResponse.EnsureSuccessStatusCode();

        using var clearResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = (string?)null, note = (string?)null } } });
        clearResponse.EnsureSuccessStatusCode();
        using var clearedRoster = JsonDocument.Parse(await clearResponse.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, Assert.Single(clearedRoster.RootElement.EnumerateArray()).GetProperty("status").ValueKind);

        using var learnerResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/learner/attendance", learnerToken);
        learnerResponse.EnsureSuccessStatusCode();
        using var learnerAttendance = JsonDocument.Parse(await learnerResponse.Content.ReadAsStringAsync());
        Assert.Empty(learnerAttendance.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Learner_CanSeeProgramAttendanceAfterBeingMarked()
    {
        var (sessionId, learnerId, learnerToken, instructorToken, programTitle) =
            await SeedProgramSessionAndEnrolledLearnerAsync();

        using var markResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/program-sessions/{sessionId}/attendance",
            instructorToken,
            new { entries = new[] { new { userId = learnerId, status = "Excused", note = "Approved absence" } } });
        markResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/learner/attendance", learnerToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Excused", record.GetProperty("status").GetString());
        Assert.Equal(programTitle, record.GetProperty("programTitle").GetString());
        Assert.Equal(JsonValueKind.Null, record.GetProperty("courseTitle").ValueKind);
    }

    private async Task<(Guid SessionId, Guid OfferingId, Guid LearnerId, string LearnerToken, string InstructorToken)> SeedOfferingSessionAndEnrolledLearnerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Attendance Cat {suffix}", Slug = $"attendance-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Attendance Course {suffix}",
            Slug = $"attendance-course-{suffix}",
            Description = "desc",
            Status = "Published",
            Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Attendance Branch {suffix}",
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

        var sessionStartsAtUtc = offering.StartsAtUtc.AddDays(1);
        using var createSessionResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{offering.Id}/sessions",
            adminToken,
            new
            {
                locationId = location.Id,
                instructorUserId = instructorId,
                title = "Attendance session",
                startsAtUtc = sessionStartsAtUtc,
                endsAtUtc = sessionStartsAtUtc.AddHours(2),
            });
        createSessionResponse.EnsureSuccessStatusCode();
        using var sessionDocument = JsonDocument.Parse(await createSessionResponse.Content.ReadAsStringAsync());
        var sessionId = sessionDocument.RootElement.GetProperty("id").GetGuid();

        var (learnerToken, learnerEmail) = await RegisterAsync();
        Guid learnerId;
        using (var enrollScope = factory.Services.CreateScope())
        {
            var userManager = enrollScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            learnerId = (await userManager.FindByEmailAsync(learnerEmail))!.Id;
        }

        // Drive the real apply -> accept -> pay pipeline so the resulting LearnerEnrollment has
        // genuine ApplicationId/PaymentId rows satisfying their FKs, instead of fabricating one.
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

        return (sessionId, offering.Id, learnerId, learnerToken, instructorToken);
    }

    private async Task<(Guid SessionId, Guid LearnerId, string LearnerToken, string InstructorToken, string ProgramTitle)>
        SeedProgramSessionAndEnrolledLearnerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var program = new AcademyProgram
        {
            Id = Guid.NewGuid(),
            Title = $"Attendance Program {suffix}",
            Slug = $"attendance-program-{suffix}",
            Description = "Program attendance integration test.",
            Status = "Published",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Program Attendance Branch {suffix}",
            RoomName = "Room 2",
            City = "Alexandria",
            Address = "Program attendance address",
            Capacity = 100,
            IsActive = true,
        };
        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(2);
        var offering = new ProgramOffering
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            LocationId = location.Id,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = startsAtUtc.AddMonths(2),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = 1_500m,
            Currency = "EGP",
            Capacity = 20,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.AddRange(program, location, offering);
        await dbContext.SaveChangesAsync();

        var (instructorToken, instructorId) = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        using var createSessionResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/program-offerings/{offering.Id}/sessions",
            adminToken,
            new
            {
                locationId = location.Id,
                instructorUserId = instructorId,
                title = "Program attendance session",
                startsAtUtc = offering.StartsAtUtc.AddDays(1),
                endsAtUtc = offering.StartsAtUtc.AddDays(1).AddHours(2),
            });
        createSessionResponse.EnsureSuccessStatusCode();
        using var sessionDocument = JsonDocument.Parse(await createSessionResponse.Content.ReadAsStringAsync());
        var sessionId = sessionDocument.RootElement.GetProperty("id").GetGuid();

        var (learnerToken, learnerEmail) = await RegisterAsync();
        Guid learnerId;
        using (var learnerScope = factory.Services.CreateScope())
        {
            var userManager = learnerScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            learnerId = (await userManager.FindByEmailAsync(learnerEmail))!.Id;
        }

        using var applyResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { programOfferingId = offering.Id });
        applyResponse.EnsureSuccessStatusCode();
        using var applyDocument = JsonDocument.Parse(await applyResponse.Content.ReadAsStringAsync());
        var applicationId = applyDocument.RootElement.GetProperty("id").GetGuid();

        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);
        acceptResponse.EnsureSuccessStatusCode();
        using var payResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });
        payResponse.EnsureSuccessStatusCode();

        return (sessionId, learnerId, learnerToken, instructorToken, program.Title);
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

    private async Task<(string AccessToken, string Email)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Attendance",
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
            firstName = "Attendance",
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
