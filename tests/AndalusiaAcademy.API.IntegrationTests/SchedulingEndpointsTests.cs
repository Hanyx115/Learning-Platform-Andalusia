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

public sealed class SchedulingEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_CanScheduleSession_AndPublicCatalogReturnsIt()
    {
        var seeded = await SeedCourseOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Orientation and foundations",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var publicResponse = await _client.GetAsync(
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions");
        publicResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync());
        var session = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Orientation and foundations", session.GetProperty("title").GetString());
        Assert.Equal(seeded.LocationId.ToString(), session.GetProperty("location").GetProperty("id").GetString());

        using var overlapResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Overlapping session",
                startsAtUtc = startsAtUtc.AddMinutes(30),
                endsAtUtc = startsAtUtc.AddHours(1),
            });

        Assert.Equal(HttpStatusCode.Conflict, overlapResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CanRescheduleSession_WithOptionalNote()
    {
        var seeded = await SeedCourseOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Original session",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });
        createResponse.EnsureSuccessStatusCode();

        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var sessionId = createDocument.RootElement.GetProperty("id").GetGuid();
        var rescheduledStart = startsAtUtc.AddDays(1);

        using var rescheduleResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/course-sessions/{sessionId}",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Rescheduled session",
                startsAtUtc = rescheduledStart,
                endsAtUtc = rescheduledStart.AddHours(2),
                note = "Moved to accommodate the cohort schedule.",
            });
        rescheduleResponse.EnsureSuccessStatusCode();

        using var rescheduleDocument = JsonDocument.Parse(await rescheduleResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Moved to accommodate the cohort schedule.",
            rescheduleDocument.RootElement.GetProperty("reschedulingNote").GetString());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedSession = await dbContext.CourseSessions.FindAsync(sessionId);
        Assert.Equal("Moved to accommodate the cohort schedule.", storedSession!.ReschedulingNote);
    }

    [Fact]
    public async Task ConcurrentOverlappingSessions_AllowOnlyOneBooking()
    {
        var seeded = await SeedCourseOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        var firstRequest = SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Concurrent session A",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });
        var secondRequest = SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Concurrent session B",
                startsAtUtc = startsAtUtc.AddMinutes(15),
                endsAtUtc = startsAtUtc.AddHours(1),
            });

        var responses = await Task.WhenAll(firstRequest, secondRequest);
        try
        {
            Assert.Equal(
                [HttpStatusCode.Created, HttpStatusCode.Conflict],
                responses.Select(response => response.StatusCode).Order().ToArray());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Learner_UpcomingSessions_ReturnsOnlyTheirEnrolledCourseSchedule()
    {
        var (learnerToken, _) = await RegisterAsync();
        var seeded = await SeedCourseOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var applicationResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/applications",
            learnerToken,
            new { courseOfferingId = seeded.OfferingId });
        applicationResponse.EnsureSuccessStatusCode();
        using var applicationDocument = JsonDocument.Parse(await applicationResponse.Content.ReadAsStringAsync());
        var applicationId = applicationDocument.RootElement.GetProperty("id").GetGuid();

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

        using var sessionResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Learner schedule session",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });
        sessionResponse.EnsureSuccessStatusCode();

        using var upcomingResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/api/v1/learner/upcoming-sessions",
            learnerToken);
        upcomingResponse.EnsureSuccessStatusCode();

        using var upcomingDocument = JsonDocument.Parse(await upcomingResponse.Content.ReadAsStringAsync());
        var session = Assert.Single(upcomingDocument.RootElement.EnumerateArray());
        Assert.Equal("Learner schedule session", session.GetProperty("sessionTitle").GetString());
        Assert.Equal(seeded.OfferingId.ToString(), session.GetProperty("courseOfferingId").GetString());
    }

    [Fact]
    public async Task Learner_CannotCreateCourseSession()
    {
        var (learnerToken, _) = await RegisterAsync();
        var seeded = await SeedCourseOfferingAsync();
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            learnerToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Forbidden session",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContentManager_CannotCreateCourseSession_UntilSchedulePermissionIsApproved()
    {
        var contentManagerToken = await RegisterAsAsync(RoleNames.ContentManager);
        var seeded = await SeedCourseOfferingAsync();
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            contentManagerToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Restricted session",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelledSession_RemainsVisibleToPublicWithCancellationFlag()
    {
        var seeded = await SeedCourseOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Session to cancel",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });
        createResponse.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var sessionId = created.RootElement.GetProperty("id").GetGuid();

        using var cancelResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/course-sessions/{sessionId}/cancel",
            adminToken,
            new { reason = "Instructor unavailable" });
        cancelResponse.EnsureSuccessStatusCode();

        using var publicResponse = await _client.GetAsync(
            $"/api/v1/course-offerings/{seeded.OfferingId}/sessions");
        publicResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync());
        var session = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Cancelled", session.GetProperty("status").GetString());
        Assert.Equal("Instructor unavailable", session.GetProperty("cancellationReason").GetString());
    }

    [Fact]
    public async Task Admin_CanCreateOnlineOffering_AndReadItsDetails()
    {
        var seeded = await SeedCourseOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        Guid courseId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            courseId = (await dbContext.CourseOfferings.FindAsync(seeded.OfferingId))!.CourseId;
        }

        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(8);
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/courses/{courseId}/offerings",
            adminToken,
            new
            {
                locationId = (Guid?)null,
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddMonths(1),
                applicationOpensAtUtc = DateTimeOffset.UtcNow,
                applicationClosesAtUtc = startsAtUtc.AddDays(-1),
                price = 900m,
                currency = "egp",
                capacity = 50,
                status = OfferingStatuses.Open,
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, created.RootElement.GetProperty("location").ValueKind);
        var offeringId = created.RootElement.GetProperty("id").GetGuid();

        using var publicResponse = await _client.GetAsync($"/api/v1/course-offerings/{offeringId}");
        publicResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Admin_CanCreateProgramSession_AndFindItInConflictReport()
    {
        var seeded = await SeedProgramOfferingAsync();
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var instructor = await RegisterAsWithUserIdAsync(RoleNames.Instructor);
        var startsAtUtc = seeded.OfferingStartsAtUtc.AddDays(1);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/program-offerings/{seeded.OfferingId}/sessions",
            adminToken,
            new
            {
                seeded.LocationId,
                instructorUserId = instructor.UserId,
                title = "Program orientation",
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddHours(2),
            });
        createResponse.EnsureSuccessStatusCode();

        var url = $"/api/v1/scheduling-conflicts?startsAtUtc={Uri.EscapeDataString(startsAtUtc.AddMinutes(-1).ToString("O"))}" +
                  $"&endsAtUtc={Uri.EscapeDataString(startsAtUtc.AddHours(3).ToString("O"))}" +
                  $"&locationId={seeded.LocationId}";
        using var conflictResponse = await SendAuthorizedAsync(HttpMethod.Get, url, adminToken);
        conflictResponse.EnsureSuccessStatusCode();
        using var conflicts = JsonDocument.Parse(await conflictResponse.Content.ReadAsStringAsync());
        var conflict = Assert.Single(conflicts.RootElement.EnumerateArray());
        Assert.Equal("Program", conflict.GetProperty("sessionType").GetString());
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

    private async Task<(Guid OfferingId, Guid LocationId, DateTimeOffset OfferingStartsAtUtc)> SeedCourseOfferingAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = $"Scheduling Category {suffix}",
            Slug = $"scheduling-category-{suffix}",
        };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Scheduling Course {suffix}",
            Slug = $"scheduling-course-{suffix}",
            Description = "Course used to verify scheduling.",
            Status = "Published",
            Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Scheduling Branch {suffix}",
            RoomName = "Room 1",
            City = "Alexandria",
            Address = "Test scheduling address",
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
            Price = 750m,
            Currency = "EGP",
            Capacity = 20,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.AddRange(category, course, location, offering);
        await dbContext.SaveChangesAsync();
        return (offering.Id, location.Id, offering.StartsAtUtc);
    }

    private async Task<(Guid OfferingId, Guid LocationId, DateTimeOffset OfferingStartsAtUtc)> SeedProgramOfferingAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var program = new AcademyProgram
        {
            Id = Guid.NewGuid(),
            Title = $"Scheduling Program {suffix}",
            Slug = $"scheduling-program-{suffix}",
            Description = "Program used to verify scheduling.",
            Status = "Published",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Program Scheduling Branch {suffix}",
            RoomName = "Room 2",
            City = "Alexandria",
            Address = "Program scheduling address",
            Capacity = 100,
            IsActive = true,
        };
        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(3);
        var offering = new ProgramOffering
        {
            Id = Guid.NewGuid(), ProgramId = program.Id, LocationId = location.Id,
            StartsAtUtc = startsAtUtc, EndsAtUtc = startsAtUtc.AddMonths(2),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
            Price = 1500m, Currency = "EGP", Capacity = 25,
            Status = OfferingStatuses.Open, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.AddRange(program, location, offering);
        await dbContext.SaveChangesAsync();
        return (offering.Id, location.Id, offering.StartsAtUtc);
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Schedule",
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
        => (await RegisterAsWithUserIdAsync(role)).AccessToken;

    private async Task<(string AccessToken, Guid UserId)> RegisterAsWithUserIdAsync(string role)
    {
        var (initialToken, email) = await RegisterAsync();
        _ = initialToken;
        const string password = "P@ssw0rd123!";

        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.AddToRoleAsync(user!, role);
            userId = user!.Id;
        }

        using var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return (document.RootElement.GetProperty("accessToken").GetString()!, userId);
    }
}
