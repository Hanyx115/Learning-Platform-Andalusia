using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Enrollment;
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
        var (accessToken, email) = await RegisterAsync();
        var (courseId, courseOfferingId) = await SeedCourseOfferingAsync();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Pending", root.GetProperty("status").GetString());
        Assert.Equal("Test", root.GetProperty("applicantFirstName").GetString());
        Assert.Equal("Learner", root.GetProperty("applicantLastName").GetString());
        Assert.Equal(email, root.GetProperty("applicantEmail").GetString());
        Assert.Equal(courseId.ToString(), root.GetProperty("courseId").GetString());
        Assert.Equal(courseOfferingId.ToString(), root.GetProperty("courseOfferingId").GetString());
        Assert.Single(root.GetProperty("statusHistory").EnumerateArray());
    }

    [Fact]
    public async Task Submit_WithBothCourseAndProgram_ReturnsValidationProblem()
    {
        var (accessToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();
        var programOfferingId = await SeedProgramOfferingAsync();

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId, programOfferingId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_Twice_ForSameCourse_ReturnsValidationProblem()
    {
        var (accessToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var first = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId });
        first.EnsureSuccessStatusCode();

        using var second = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Submit_Concurrently_ForSameCourse_CreatesOnlyOneApplication()
    {
        var (accessToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        var responses = await Task.WhenAll(
            SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId }),
            SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId }));

        try
        {
            Assert.Equal(
                [HttpStatusCode.OK, HttpStatusCode.BadRequest],
                responses.Select(response => response.StatusCode).Order().ToArray());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            dbContext.LearnerApplications.Count(application =>
                application.CourseOfferingId == courseOfferingId &&
                application.Status == ApplicationStatus.Pending));
    }

    [Fact]
    public async Task Submit_AfterSameOfferingWasAccepted_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var first = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
        first.EnsureSuccessStatusCode();
        using var firstDocument = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var applicationId = firstDocument.RootElement.GetProperty("id").GetString();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        using var accept = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);
        accept.EnsureSuccessStatusCode();

        using var duplicate = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task Submit_AfterSameOfferingWasRejected_CreatesANewApplication()
    {
        var (learnerToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var first = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
        first.EnsureSuccessStatusCode();
        using var firstDocument = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var applicationId = firstDocument.RootElement.GetProperty("id").GetString();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        using var reject = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/applications/{applicationId}/reject",
            adminToken,
            new { reason = "Please correct the application." });
        reject.EnsureSuccessStatusCode();

        using var corrected = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });

        corrected.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnApplications()
    {
        var (accessToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", accessToken, new { courseOfferingId });
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
    public async Task GetPage_FiltersSortsAndPaginatesPendingApplications()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstIntake = new DateTimeOffset(DateTime.UtcNow.Date.AddMonths(3), TimeSpan.Zero);
        var secondIntake = firstIntake.AddDays(10);

        var (zeldaToken, _) = await RegisterAsync($"Zelda{suffix}", "Applicant");
        var (_, alphaOfferingId) = await SeedCourseOfferingAsync($"Filtering Alpha {suffix}", firstIntake);
        using (var alphaSubmit = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", zeldaToken, new { courseOfferingId = alphaOfferingId }))
        {
            alphaSubmit.EnsureSuccessStatusCode();
        }

        var (annaToken, _) = await RegisterAsync($"Anna{suffix}", "Applicant");
        var (_, zuluOfferingId) = await SeedCourseOfferingAsync($"Filtering Zulu {suffix}", secondIntake);
        using (var zuluSubmit = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", annaToken, new { courseOfferingId = zuluOfferingId }))
        {
            zuluSubmit.EnsureSuccessStatusCode();
        }

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var encodedSuffix = Uri.EscapeDataString(suffix);

        using var learnerPage = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/applications?status=Pending&learnerName={encodedSuffix}&sort=learner_asc&page=1&pageSize=1",
            adminToken);
        learnerPage.EnsureSuccessStatusCode();
        using var learnerDocument = JsonDocument.Parse(await learnerPage.Content.ReadAsStringAsync());
        Assert.Equal(2, learnerDocument.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, learnerDocument.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Equal(
            $"Anna{suffix}",
            learnerDocument.RootElement.GetProperty("items")[0].GetProperty("applicantFirstName").GetString());

        using var secondPage = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/applications?status=Pending&learnerName={encodedSuffix}&sort=learner_asc&page=2&pageSize=1",
            adminToken);
        secondPage.EnsureSuccessStatusCode();
        using var secondDocument = JsonDocument.Parse(await secondPage.Content.ReadAsStringAsync());
        Assert.Equal(
            $"Zelda{suffix}",
            secondDocument.RootElement.GetProperty("items")[0].GetProperty("applicantFirstName").GetString());

        using var offeringPage = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/applications?status=Pending&offeringTitle={Uri.EscapeDataString($"Alpha {suffix}")}",
            adminToken);
        offeringPage.EnsureSuccessStatusCode();
        using var offeringDocument = JsonDocument.Parse(await offeringPage.Content.ReadAsStringAsync());
        Assert.Equal(1, offeringDocument.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(
            $"Filtering Alpha {suffix}",
            offeringDocument.RootElement.GetProperty("items")[0].GetProperty("courseTitle").GetString());

        var intakeDate = firstIntake.ToString("yyyy-MM-dd");
        using var intakePage = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/applications?status=Pending&learnerName={encodedSuffix}&intakeFrom={intakeDate}&intakeTo={intakeDate}",
            adminToken);
        intakePage.EnsureSuccessStatusCode();
        using var intakeDocument = JsonDocument.Parse(await intakePage.Content.ReadAsStringAsync());
        Assert.Equal(1, intakeDocument.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(
            $"Filtering Alpha {suffix}",
            intakeDocument.RootElement.GetProperty("items")[0].GetProperty("courseTitle").GetString());

        using var offeringSortPage = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/applications?status=Pending&learnerName={encodedSuffix}&sort=offering_desc&page=1&pageSize=1",
            adminToken);
        offeringSortPage.EnsureSuccessStatusCode();
        using var offeringSortDocument = JsonDocument.Parse(await offeringSortPage.Content.ReadAsStringAsync());
        Assert.Equal(
            $"Filtering Zulu {suffix}",
            offeringSortDocument.RootElement.GetProperty("items")[0].GetProperty("courseTitle").GetString());
    }

    [Fact]
    public async Task Accept_AsAdmin_TransitionsApplicationToAccepted()
    {
        var (learnerToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetString();

        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);
        acceptResponse.EnsureSuccessStatusCode();

        using var acceptDocument = JsonDocument.Parse(await acceptResponse.Content.ReadAsStringAsync());
        Assert.Equal("Accepted", acceptDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, acceptDocument.RootElement.GetProperty("statusHistory").GetArrayLength());
    }

    [Fact]
    public async Task Reject_ThenAcceptAgain_ReturnsNotFoundSinceNoLongerPending()
    {
        var (learnerToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
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
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var submitResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
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
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/applications", adminOnlyToken, new { courseOfferingId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_ForOfferingAtInactiveLocation_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var (_, courseOfferingId) = await SeedCourseOfferingAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var offering = await dbContext.CourseOfferings.FindAsync(courseOfferingId);
            var location = await dbContext.TrainingLocations.FindAsync(offering!.LocationId);
            location!.IsActive = false;
            await dbContext.SaveChangesAsync();
        }

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private async Task<(Guid CourseId, Guid OfferingId)> SeedCourseOfferingAsync(
        string? courseTitle = null,
        DateTimeOffset? startsAtUtc = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var intakeStartsAtUtc = startsAtUtc ?? DateTimeOffset.UtcNow.AddMonths(2);

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Cat-{Guid.NewGuid()}", Slug = $"cat-{Guid.NewGuid()}" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = courseTitle ?? $"Course-{Guid.NewGuid()}",
            Slug = $"course-{Guid.NewGuid()}",
            Description = "Test course.",
            Status = "Published",
            Type = "Online",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.CourseCategories.Add(category);
        dbContext.Courses.Add(course);
        var location = CreateLocation();
        dbContext.TrainingLocations.Add(location);
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = intakeStartsAtUtc,
            EndsAtUtc = intakeStartsAtUtc.AddMonths(2),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = 500m,
            Currency = "EGP",
            Capacity = 20,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.CourseOfferings.Add(offering);
        await dbContext.SaveChangesAsync();

        return (course.Id, offering.Id);
    }

    private async Task<Guid> SeedProgramOfferingAsync()
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
        var location = CreateLocation();
        dbContext.TrainingLocations.Add(location);
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
        dbContext.ProgramOfferings.Add(offering);
        await dbContext.SaveChangesAsync();

        return offering.Id;
    }

    private static TrainingLocation CreateLocation()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Test Branch {suffix}",
            RoomName = "Room 1",
            City = "Alexandria",
            Address = "Test address",
            Capacity = 100,
            IsActive = true,
        };
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync(
        string firstName = "Test",
        string lastName = "Learner")
    {
        var email = $"{Guid.NewGuid()}@example.com";

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "P@ssw0rd123!",
            firstName,
            lastName,
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
