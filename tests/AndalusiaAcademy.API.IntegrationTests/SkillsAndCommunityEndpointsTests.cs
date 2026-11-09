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

public sealed class SkillsAndCommunityEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LearnerPassport_UsesOnlyOwnGradedAssignmentEvidence()
    {
        var seed = await SeedEnrolledOfferingAsync();
        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/course-offerings/{seed.OfferingId}/assignments", seed.AdminToken,
            new { title = "Threat model", description = "Assess the scenario.", dueAtUtc = DateTimeOffset.UtcNow.AddDays(7), maxScore = 100 });
        createResponse.EnsureSuccessStatusCode();
        var assignmentId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/assignments/{assignmentId}/submit", seed.LearnerToken,
            new { content = "My evidence" });
        submitResponse.EnsureSuccessStatusCode();
        using var gradeResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/assignments/{assignmentId}/submissions/{seed.LearnerId}/grade", seed.InstructorToken,
            new { score = 82, feedback = "Good analysis" });
        gradeResponse.EnsureSuccessStatusCode();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/skills/passport/me", seed.LearnerToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var skill = Assert.Single(document.RootElement.GetProperty("skills").EnumerateArray());
        Assert.StartsWith("Threat awareness", skill.GetProperty("name").GetString());
        Assert.Equal(82, skill.GetProperty("masteryPercentage").GetDecimal());
        Assert.Equal("Demonstrated", skill.GetProperty("status").GetString());
        Assert.Equal(1, skill.GetProperty("gradedEvidenceCount").GetInt32());
    }

    [Fact]
    public async Task CourseCommunity_EnforcesMembershipAndInstructorModeration()
    {
        var seed = await SeedEnrolledOfferingAsync();
        var outsider = await RegisterAsync("Outside", "Learner");

        using var outsiderResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/community/course/{seed.OfferingId}", outsider.Token);
        Assert.Equal(HttpStatusCode.NotFound, outsiderResponse.StatusCode);

        using var learnerPostResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/community/course/{seed.OfferingId}/posts", seed.LearnerToken,
            new { postType = "Discussion", title = "Access control question", body = "Can someone explain least privilege?" });
        Assert.Equal(HttpStatusCode.Created, learnerPostResponse.StatusCode);
        var learnerPostId = (await learnerPostResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var fakeAnnouncementResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/community/course/{seed.OfferingId}/posts", seed.LearnerToken,
            new { postType = "Announcement", title = "Fake announcement", body = "This must be rejected." });
        Assert.Equal(HttpStatusCode.Forbidden, fakeAnnouncementResponse.StatusCode);

        using var announcementResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/community/course/{seed.OfferingId}/posts", seed.InstructorToken,
            new { postType = "Announcement", title = "Next workshop", body = "Bring your threat models." });
        Assert.Equal(HttpStatusCode.Created, announcementResponse.StatusCode);

        using var lockResponse = await SendAuthorizedAsync(
            HttpMethod.Patch, $"/api/v1/community/posts/{learnerPostId}/moderation", seed.InstructorToken,
            new { isPinned = true, isLocked = true });
        lockResponse.EnsureSuccessStatusCode();

        using var lockedReplyResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/community/posts/{learnerPostId}/replies", seed.LearnerToken,
            new { body = "This reply must be blocked." });
        Assert.Equal(HttpStatusCode.Conflict, lockedReplyResponse.StatusCode);

        using var pageResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/community/course/{seed.OfferingId}", seed.LearnerToken);
        pageResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await pageResponse.Content.ReadAsStringAsync());
        Assert.False(document.RootElement.GetProperty("canAnnounce").GetBoolean());
        Assert.Equal(2, document.RootElement.GetProperty("totalCount").GetInt32());
        var posts = document.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.True(posts[0].GetProperty("isPinned").GetBoolean());
        Assert.Contains(posts, post => post.GetProperty("postType").GetString() == "Announcement");
    }

    private async Task<Seed> SeedEnrolledOfferingAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        Guid offeringId;
        Guid skillId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Community {suffix}", Slug = $"community-{suffix}" };
            var course = new Course
            {
                Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Secure Learning {suffix}", Slug = $"secure-learning-{suffix}",
                Description = "Security learning", Status = "Published", Type = "Online", CreatedAt = DateTimeOffset.UtcNow,
            };
            var location = new TrainingLocation
            {
                Id = Guid.NewGuid(), BranchName = $"Online {suffix}", RoomName = "Virtual", City = "Online",
                Address = "Online", Capacity = 50, IsActive = true,
            };
            var offering = new CourseOffering
            {
                Id = Guid.NewGuid(), CourseId = course.Id, LocationId = location.Id,
                StartsAtUtc = DateTimeOffset.UtcNow.AddDays(10), EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
                ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1), ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddDays(5),
                Price = 100, Currency = "EGP", Capacity = 20, Status = OfferingStatuses.Open, CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            var skill = new Skill
            {
                Id = Guid.NewGuid(), Name = $"Threat awareness {suffix}", Category = "Cybersecurity",
                Description = "Recognize threats.", IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            dbContext.AddRange(category, course, location, offering, skill, new LearningSkillMapping
            {
                Id = Guid.NewGuid(), SkillId = skill.Id, CourseId = course.Id, TargetLevel = 2,
            });
            await dbContext.SaveChangesAsync();
            offeringId = offering.Id;
            skillId = skill.Id;
        }

        var instructor = await RegisterAsAsync(RoleNames.Instructor, "Course", "Instructor");
        var admin = await RegisterAsAsync(RoleNames.Admin, "Academy", "Admin");
        var learner = await RegisterAsync("Skills", "Learner");
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.CourseSessions.Add(new CourseSession
            {
                Id = Guid.NewGuid(), CourseOfferingId = offeringId, InstructorUserId = instructor.UserId,
                Title = "Community session", StartsAtUtc = DateTimeOffset.UtcNow.AddDays(12),
                EndsAtUtc = DateTimeOffset.UtcNow.AddDays(12).AddHours(2), Status = CourseSessionStatuses.Scheduled,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await dbContext.SaveChangesAsync();
        }

        using var applyResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learner.Token, new { courseOfferingId = offeringId });
        applyResponse.EnsureSuccessStatusCode();
        var applicationId = (await applyResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", admin.Token);
        acceptResponse.EnsureSuccessStatusCode();
        using var payResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", learner.Token, new { applicationId });
        payResponse.EnsureSuccessStatusCode();

        return new Seed(offeringId, skillId, learner.UserId, learner.Token, instructor.Token, admin.Token);
    }

    private async Task<UserSession> RegisterAsync(string firstName, string lastName)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password, firstName, lastName, phoneNumber = "+201234567890", countryCode = "EG", preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        using var scope = factory.Services.CreateScope();
        var user = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!;
        return new UserSession(token, user.Id);
    }

    private async Task<UserSession> RegisterAsAsync(string role, string firstName, string lastName)
    {
        var session = await RegisterAsync(firstName, lastName);
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.AddToRoleAsync((await userManager.FindByIdAsync(session.UserId.ToString()))!, role);
        var user = await userManager.FindByIdAsync(session.UserId.ToString());
        var email = user!.Email!;
        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "P@ssw0rd123!" });
        loginResponse.EnsureSuccessStatusCode();
        return session with { Token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()! };
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private sealed record UserSession(string Token, Guid UserId);
    private sealed record Seed(Guid OfferingId, Guid SkillId, Guid LearnerId, string LearnerToken, string InstructorToken, string AdminToken);
}
