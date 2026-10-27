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

public sealed class AssignmentsEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_CanCreateAssignment_AndAssignedInstructorCanSeeIt()
    {
        var (offeringId, _, _, instructorToken, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();

        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);

        using var listResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-offerings/{offeringId}/assignments", instructorToken);
        listResponse.EnsureSuccessStatusCode();
        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var assignment = Assert.Single(listDocument.RootElement.EnumerateArray());
        Assert.Equal(assignmentId.ToString(), assignment.GetProperty("id").GetString());
    }

    [Fact]
    public async Task UnassignedInstructor_CannotSeeOfferingAssignments()
    {
        var (offeringId, _, _, _, _) = await SeedOfferingWithEnrolledLearnerAsync();
        await CreateAssignmentAsync(offeringId, await RegisterAsAsync(RoleNames.Admin));
        var otherInstructorToken = await RegisterAsAsync(RoleNames.Instructor);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/course-offerings/{offeringId}/assignments", otherInstructorToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Learner_CanSubmit_AndInstructorCanSeeAndGradeIt()
    {
        var (offeringId, learnerId, learnerToken, instructorToken, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);

        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit-link", learnerToken,
            new { url = "https://example.com/my-essay.pdf" });
        submitResponse.EnsureSuccessStatusCode();

        using var submissionsResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/assignments/{assignmentId}/submissions", instructorToken);
        submissionsResponse.EnsureSuccessStatusCode();
        using var submissionsDocument = JsonDocument.Parse(await submissionsResponse.Content.ReadAsStringAsync());
        var submissionEntry = Assert.Single(submissionsDocument.RootElement.EnumerateArray());
        Assert.Equal(learnerId.ToString(), submissionEntry.GetProperty("userId").GetString());
        Assert.Equal("https://example.com/my-essay.pdf", submissionEntry.GetProperty("content").GetString());
        Assert.Equal("Link", submissionEntry.GetProperty("submissionType").GetString());
        Assert.Equal("https://example.com/my-essay.pdf", submissionEntry.GetProperty("linkUrl").GetString());
        Assert.Equal(JsonValueKind.Null, submissionEntry.GetProperty("score").ValueKind);

        using var gradeResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/assignments/{assignmentId}/submissions/{learnerId}/grade", instructorToken,
            new { score = 85, feedback = "Well done." });
        gradeResponse.EnsureSuccessStatusCode();
        using var gradeDocument = JsonDocument.Parse(await gradeResponse.Content.ReadAsStringAsync());
        Assert.Equal(85, gradeDocument.RootElement.GetProperty("score").GetDecimal());
        Assert.Equal("Well done.", gradeDocument.RootElement.GetProperty("feedback").GetString());
    }

    [Fact]
    public async Task Learner_CanUploadDocument_AndLearnerAndInstructorCanDownloadIt()
    {
        var (offeringId, learnerId, learnerToken, instructorToken, adminToken) =
            await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);
        var documentBytes = "%PDF-1.7 demo assignment"u8.ToArray();

        using var submitRequest = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit-file");
        submitRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", learnerToken);
        using var multipart = new MultipartFormDataContent();
        using var documentContent = new ByteArrayContent(documentBytes);
        documentContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        multipart.Add(documentContent, "file", "reflection.pdf");
        submitRequest.Content = multipart;

        using var submitResponse = await _client.SendAsync(submitRequest);
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        Assert.Equal("File", submitDocument.RootElement.GetProperty("submissionType").GetString());
        Assert.Equal("reflection.pdf", submitDocument.RootElement.GetProperty("originalFileName").GetString());

        using var learnerDownload = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/assignments/{assignmentId}/submission/file", learnerToken);
        learnerDownload.EnsureSuccessStatusCode();
        Assert.Equal(documentBytes, await learnerDownload.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", learnerDownload.Content.Headers.ContentType?.MediaType);

        using var instructorDownload = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/assignments/{assignmentId}/submissions/{learnerId}/file",
            instructorToken);
        instructorDownload.EnsureSuccessStatusCode();
        Assert.Equal(documentBytes, await instructorDownload.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Learner_CannotUploadExecutableAsAssignmentDocument()
    {
        var (offeringId, _, learnerToken, _, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit-file");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", learnerToken);
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent("not safe"u8.ToArray()), "file", "homework.exe");
        request.Content = multipart;

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Grade_AboveMaxScore_ReturnsValidationProblem()
    {
        var (offeringId, learnerId, learnerToken, instructorToken, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken, maxScore: 100);

        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", learnerToken, new { content = "My work" });
        submitResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/assignments/{assignmentId}/submissions/{learnerId}/grade", instructorToken,
            new { score = 150, feedback = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resubmit_AfterGrading_ReturnsConflict()
    {
        var (offeringId, learnerId, learnerToken, instructorToken, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);

        using var firstSubmit = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", learnerToken, new { content = "Draft 1" });
        firstSubmit.EnsureSuccessStatusCode();

        using var gradeResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/assignments/{assignmentId}/submissions/{learnerId}/grade", instructorToken,
            new { score = 90, feedback = (string?)null });
        gradeResponse.EnsureSuccessStatusCode();

        using var secondSubmit = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", learnerToken, new { content = "Draft 2" });

        Assert.Equal(HttpStatusCode.Conflict, secondSubmit.StatusCode);
    }

    [Fact]
    public async Task Learner_SeesAssignmentAndGrade_InMyAssignments()
    {
        var (offeringId, learnerId, learnerToken, instructorToken, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);

        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", learnerToken, new { content = "My submission" });
        submitResponse.EnsureSuccessStatusCode();

        using var gradeResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/assignments/{assignmentId}/submissions/{learnerId}/grade", instructorToken,
            new { score = 72, feedback = "Good effort." });
        gradeResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/learner/assignments", learnerToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entry = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(assignmentId.ToString(), entry.GetProperty("assignmentId").GetString());
        Assert.Equal(72, entry.GetProperty("myScore").GetDecimal());
        Assert.Equal("Good effort.", entry.GetProperty("myFeedback").GetString());
    }

    [Fact]
    public async Task UnenrolledLearner_CannotSubmit()
    {
        var (offeringId, _, _, _, adminToken) = await SeedOfferingWithEnrolledLearnerAsync();
        var assignmentId = await CreateAssignmentAsync(offeringId, adminToken);
        var (strangerToken, _) = await RegisterAsync();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", strangerToken, new { content = "Not enrolled" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateAssignmentAsync(Guid offeringId, string adminToken, decimal maxScore = 100)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/course-offerings/{offeringId}/assignments", adminToken,
            new
            {
                title = "Essay assignment",
                description = "Write a 500-word essay.",
                dueAtUtc = DateTimeOffset.UtcNow.AddDays(14),
                maxScore,
            });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<(Guid OfferingId, Guid LearnerId, string LearnerToken, string InstructorToken, string AdminToken)>
        SeedOfferingWithEnrolledLearnerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Assignment Cat {suffix}", Slug = $"assignment-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Assignment Course {suffix}",
            Slug = $"assignment-course-{suffix}", Description = "desc", Status = "Published", Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(), BranchName = $"Assignment Branch {suffix}", RoomName = "Room 1",
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
                title = "Assignment seed session",
                startsAtUtc = sessionStartsAtUtc,
                endsAtUtc = sessionStartsAtUtc.AddHours(2),
            });
        createSessionResponse.EnsureSuccessStatusCode();

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

        return (offering.Id, learnerId, learnerToken, instructorToken, adminToken);
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
            firstName = "Assignment",
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
            firstName = "Assignment",
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
