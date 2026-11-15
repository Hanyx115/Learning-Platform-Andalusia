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

public sealed class AdminDirectoryEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_CanListAllEnrollmentsAndPayments()
    {
        var seed = await SeedEnrolledLearnerAsync();

        using var enrollmentsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/enrollments", seed.AdminToken);
        enrollmentsResponse.EnsureSuccessStatusCode();
        using var enrollmentsDocument = JsonDocument.Parse(await enrollmentsResponse.Content.ReadAsStringAsync());
        var enrollmentEntry = enrollmentsDocument.RootElement.EnumerateArray()
            .Single(entry => entry.GetProperty("id").GetGuid() == seed.EnrollmentId);
        Assert.Equal(seed.LearnerId.ToString(), enrollmentEntry.GetProperty("userId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(enrollmentEntry.GetProperty("learnerName").GetString()));

        using var paymentsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/payments", seed.AdminToken);
        paymentsResponse.EnsureSuccessStatusCode();
        using var paymentsDocument = JsonDocument.Parse(await paymentsResponse.Content.ReadAsStringAsync());
        var paymentEntry = paymentsDocument.RootElement.EnumerateArray()
            .Single(entry => entry.GetProperty("id").GetGuid() == seed.PaymentId);
        Assert.Equal(seed.LearnerId.ToString(), paymentEntry.GetProperty("userId").GetString());
        Assert.Equal("Succeeded", paymentEntry.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Learner_CannotListAllEnrollments()
    {
        var (learnerToken, _) = await RegisterAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/enrollments", learnerToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CanViewOperationsOverview_WhileLearnerIsForbidden()
    {
        var seed = await SeedEnrolledLearnerAsync();
        var managerToken = await RegisterAsAsync(RoleNames.Manager);

        using var managerResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/operations/overview", managerToken);
        managerResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await managerResponse.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("activeStudents").GetInt32() >= 1);
        Assert.True(document.RootElement.GetProperty("activeInstructors").GetInt32() >= 0);
        Assert.True(document.RootElement.GetProperty("activeEnrollments").GetInt32() >= 1);
        Assert.Contains(
            document.RootElement.GetProperty("revenue").EnumerateArray(),
            entry => entry.GetProperty("currency").GetString() == "EGP"
                && entry.GetProperty("amount").GetDecimal() >= 500m);
        Assert.Contains(
            document.RootElement.GetProperty("recentActivity").EnumerateArray(),
            entry => entry.GetProperty("id").GetGuid() == seed.EnrollmentId
                && entry.GetProperty("activityType").GetString() == "Enrollment");

        var (learnerToken, _) = await RegisterAsync();
        using var learnerResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/operations/overview", learnerToken);
        Assert.Equal(HttpStatusCode.Forbidden, learnerResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CanListUsersInInstructorRole()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var (_, instructorId) = await RegisterAsWithUserIdAsync(RoleNames.Instructor);

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/profile/by-role/Instructor", adminToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            document.RootElement.EnumerateArray(),
            entry => entry.GetProperty("id").GetGuid() == instructorId);
    }

    [Fact]
    public async Task Manager_CanSearchAndFilterThePaginatedStudentDirectory()
    {
        var seed = await SeedEnrolledLearnerAsync();
        var managerToken = await RegisterAsAsync(RoleNames.Manager);
        var search = Uri.EscapeDataString(seed.CourseTitle);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/enrollments/manage?search={search}&status=Active&type=Course&page=1&pageSize=1",
            managerToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, document.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("pageSize").GetInt32());
        Assert.True(document.RootElement.GetProperty("totalCount").GetInt32() >= 1);
        var entry = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(seed.EnrollmentId, entry.GetProperty("id").GetGuid());
        Assert.Equal(seed.LearnerEmail, entry.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Admin_CanSearchFilterAndPaginateTheInstructorDirectory()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, activeInstructorId) = await RegisterAsWithUserIdAsync(
            RoleNames.Instructor, $"Directory{suffix}", "Active");
        var (_, inactiveInstructorId) = await RegisterAsWithUserIdAsync(
            RoleNames.Instructor, $"Directory{suffix}", "Inactive");

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var inactiveInstructor = (await userManager.FindByIdAsync(inactiveInstructorId.ToString()))!;
            inactiveInstructor.IsActive = false;
            Assert.True((await userManager.UpdateAsync(inactiveInstructor)).Succeeded);
        }

        using var pageResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/profile/by-role/Instructor/manage?search={suffix}&page=1&pageSize=1",
            adminToken);
        pageResponse.EnsureSuccessStatusCode();
        using var pageDocument = JsonDocument.Parse(await pageResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, pageDocument.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, pageDocument.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Single(pageDocument.RootElement.GetProperty("items").EnumerateArray());

        using var activeResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/profile/by-role/Instructor/manage?search={suffix}&status=Active",
            adminToken);
        activeResponse.EnsureSuccessStatusCode();
        using var activeDocument = JsonDocument.Parse(await activeResponse.Content.ReadAsStringAsync());
        var activeEntry = Assert.Single(activeDocument.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(activeInstructorId, activeEntry.GetProperty("id").GetGuid());
        Assert.True(activeEntry.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Admin_CanListAllOfferingsForACourse_IncludingNonOpenStatus()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var suffix = Guid.NewGuid().ToString("N");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Admin Dir Cat {suffix}", Slug = $"admin-dir-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Admin Dir Course {suffix}",
            Slug = $"admin-dir-course-{suffix}", Description = "desc", Status = "Published", Type = "Offline",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.AddRange(category, course);
        await dbContext.SaveChangesAsync();

        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(3);
        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/courses/{course.Id}/offerings", adminToken,
            new
            {
                locationId = (Guid?)null,
                startsAtUtc,
                endsAtUtc = startsAtUtc.AddMonths(1),
                applicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(10),
                applicationClosesAtUtc = startsAtUtc.AddDays(-1),
                price = 300m,
                currency = "EGP",
                capacity = 10,
                status = "Draft",
            });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var offeringId = createDocument.RootElement.GetProperty("id").GetGuid();

        using var manageResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/courses/{course.Id}/offerings/manage", adminToken);
        manageResponse.EnsureSuccessStatusCode();
        using var manageDocument = JsonDocument.Parse(await manageResponse.Content.ReadAsStringAsync());
        var entry = Assert.Single(manageDocument.RootElement.EnumerateArray());
        Assert.Equal(offeringId.ToString(), entry.GetProperty("id").GetString());
        Assert.Equal("Draft", entry.GetProperty("status").GetString());
    }

    private sealed record EnrolledLearnerSeed(
        Guid EnrollmentId,
        Guid PaymentId,
        Guid LearnerId,
        string LearnerEmail,
        string CourseTitle,
        string AdminToken);

    private async Task<EnrolledLearnerSeed> SeedEnrolledLearnerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Admin Seed Cat {suffix}", Slug = $"admin-seed-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Admin Seed Course {suffix}",
            Slug = $"admin-seed-course-{suffix}", Description = "desc", Status = "Published", Type = "Online",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var startsAtUtc = DateTimeOffset.UtcNow.AddMonths(2);
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(), CourseId = course.Id, LocationId = null,
            StartsAtUtc = startsAtUtc, EndsAtUtc = startsAtUtc.AddMonths(2),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = 500m, Currency = "EGP", Capacity = 20, Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.AddRange(category, course, offering);
        await dbContext.SaveChangesAsync();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
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
        using var payDocument = JsonDocument.Parse(await payResponse.Content.ReadAsStringAsync());
        var enrollmentId = payDocument.RootElement.GetProperty("enrollment").GetProperty("id").GetGuid();
        var paymentId = payDocument.RootElement.GetProperty("payment").GetProperty("id").GetGuid();

        Guid learnerId;
        using (var userScope = factory.Services.CreateScope())
        {
            var userManager = userScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            learnerId = (await userManager.FindByEmailAsync(learnerEmail))!.Id;
        }

        return new EnrolledLearnerSeed(enrollmentId, paymentId, learnerId, learnerEmail, course.Title, adminToken);
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
            firstName = "AdminDir",
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

    private async Task<(string AccessToken, Guid UserId)> RegisterAsWithUserIdAsync(
        string role,
        string firstName = "AdminDir",
        string lastName = "Staff")
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName,
            lastName,
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
