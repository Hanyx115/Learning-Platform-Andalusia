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

public sealed class PaymentsEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Pay_ForAcceptedApplication_SucceedsAndCreatesEnrollment()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 499.99m);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("Succeeded", root.GetProperty("payment").GetProperty("status").GetString());
        Assert.Equal(499.99, root.GetProperty("payment").GetProperty("amount").GetDouble());
        Assert.Equal("Active", root.GetProperty("enrollment").GetProperty("status").GetString());

        using var enrollmentsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/enrollments/me", learnerToken);
        enrollmentsResponse.EnsureSuccessStatusCode();
        using var enrollmentsDocument = JsonDocument.Parse(await enrollmentsResponse.Content.ReadAsStringAsync());
        Assert.True(enrollmentsDocument.RootElement.GetArrayLength() >= 1);

        using var paymentsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/payments/me", learnerToken);
        paymentsResponse.EnsureSuccessStatusCode();
        using var paymentsDocument = JsonDocument.Parse(await paymentsResponse.Content.ReadAsStringAsync());
        Assert.True(paymentsDocument.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Pay_Twice_ForSameApplication_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);

        using var first = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });
        first.EnsureSuccessStatusCode();

        using var second = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Pay_ForStillPendingApplication_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);

        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetString();

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pay_UsesOfferingPriceWhenCatalogPriceIsMissing()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 250m, catalogPrice: null);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(250m, document.RootElement.GetProperty("payment").GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Pay_ForSomeoneElsesApplication_ReturnsValidationProblem()
    {
        var (ownerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);
        var applicationId = await SubmitAndAcceptAsync(ownerToken, courseOfferingId);

        var (otherLearnerToken, _) = await RegisterAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", otherLearnerToken, new { applicationId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pay_AsAdminWithoutLearnerRole_ReturnsForbidden()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);

        var adminOnlyToken = await RegisterAdminOnlyAsync();

        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/payments", adminOnlyToken, new { applicationId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pay_WhenOfferingIsFull_ReturnsValidationProblemAndDoesNotCreateEnrollment()
    {
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m, capacity: 1);
        var (firstLearnerToken, _) = await RegisterAsync();
        var (secondLearnerToken, _) = await RegisterAsync();
        var firstApplicationId = await SubmitAndAcceptAsync(firstLearnerToken, courseOfferingId);
        var secondApplicationId = await SubmitAndAcceptAsync(secondLearnerToken, courseOfferingId);

        using var firstPayment = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", firstLearnerToken, new { applicationId = firstApplicationId });
        firstPayment.EnsureSuccessStatusCode();

        using var secondPayment = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", secondLearnerToken, new { applicationId = secondApplicationId });

        Assert.Equal(HttpStatusCode.BadRequest, secondPayment.StatusCode);
        using var enrollments = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/v1/enrollments/me", secondLearnerToken);
        enrollments.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await enrollments.Content.ReadAsStringAsync());
        Assert.Empty(document.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task ConcurrentPayments_ForLastSeat_CreateOnlyOneEnrollment()
    {
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m, capacity: 1);
        var (firstLearnerToken, _) = await RegisterAsync();
        var (secondLearnerToken, _) = await RegisterAsync();
        var firstApplicationId = await SubmitAndAcceptAsync(firstLearnerToken, courseOfferingId);
        var secondApplicationId = await SubmitAndAcceptAsync(secondLearnerToken, courseOfferingId);

        var responses = await Task.WhenAll(
            SendAuthorizedAsync(
                HttpMethod.Post, "/api/v1/payments", firstLearnerToken, new { applicationId = firstApplicationId }),
            SendAuthorizedAsync(
                HttpMethod.Post, "/api/v1/payments", secondLearnerToken, new { applicationId = secondApplicationId }));

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
            dbContext.Enrollments.Count(enrollment =>
                enrollment.CourseOfferingId == courseOfferingId && enrollment.Status == EnrollmentStatus.Active));
    }

    [Fact]
    public async Task Pay_AfterOfferingWasCancelled_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);
        await UpdateOfferingAsync(courseOfferingId, offering => offering.Status = OfferingStatuses.Cancelled);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pay_AfterPhysicalLocationWasDeactivated_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var offering = await dbContext.CourseOfferings.FindAsync(courseOfferingId);
            var location = await dbContext.TrainingLocations.FindAsync(offering!.LocationId);
            location!.IsActive = false;
            await dbContext.SaveChangesAsync();
        }

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pay_AfterOfferingEnded_ReturnsValidationProblem()
    {
        var (learnerToken, _) = await RegisterAsync();
        var courseOfferingId = await SeedCourseOfferingAsync(offeringPrice: 100m);
        var applicationId = await SubmitAndAcceptAsync(learnerToken, courseOfferingId);
        await UpdateOfferingAsync(courseOfferingId, offering =>
        {
            offering.ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-10);
            offering.ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddDays(-4);
            offering.StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-3);
            offering.EndsAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        });

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/payments", learnerToken, new { applicationId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private async Task<string> SubmitAndAcceptAsync(string learnerToken, Guid courseOfferingId)
    {
        using var submitResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/applications", learnerToken, new { courseOfferingId });
        submitResponse.EnsureSuccessStatusCode();
        using var submitDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        var applicationId = submitDocument.RootElement.GetProperty("id").GetString()!;

        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        using var acceptResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/applications/{applicationId}/accept", adminToken);
        acceptResponse.EnsureSuccessStatusCode();

        return applicationId;
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

    private async Task<Guid> SeedCourseOfferingAsync(
        decimal offeringPrice,
        decimal? catalogPrice = null,
        int capacity = 20)
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
            Price = catalogPrice,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.CourseCategories.Add(category);
        dbContext.Courses.Add(course);
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Test Branch {Guid.NewGuid():N}",
            RoomName = "Virtual Room",
            City = "Online",
            Address = "Online",
            Capacity = 100,
            IsActive = true,
        };
        dbContext.TrainingLocations.Add(location);
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
            EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(4),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Price = offeringPrice,
            Currency = "EGP",
            Capacity = capacity,
            Status = OfferingStatuses.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.CourseOfferings.Add(offering);
        await dbContext.SaveChangesAsync();

        return offering.Id;
    }

    private async Task UpdateOfferingAsync(Guid offeringId, Action<CourseOffering> update)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var offering = await dbContext.CourseOfferings.FindAsync(offeringId)
            ?? throw new InvalidOperationException("Test offering was not found.");
        update(offering);
        await dbContext.SaveChangesAsync();
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
}
