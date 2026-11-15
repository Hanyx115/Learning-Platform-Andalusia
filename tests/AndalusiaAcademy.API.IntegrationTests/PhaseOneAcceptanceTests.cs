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

public sealed class PhaseOneAcceptanceTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task VisitorToLearnerJourney_CompletesOnCleanDatabase()
    {
        var unique = Guid.NewGuid().ToString("N");
        var offeringId = await SeedPublishedOfferingAsync(unique);

        using (var catalogResponse = await _client.GetAsync($"/api/v1/courses?search=Acceptance-{unique}&page=1&pageSize=10"))
        {
            catalogResponse.EnsureSuccessStatusCode();
            using var catalog = JsonDocument.Parse(await catalogResponse.Content.ReadAsStringAsync());
            Assert.Equal(1, catalog.RootElement.GetProperty("totalCount").GetInt32());
        }

        var learnerToken = await RegisterAsync($"learner-{unique}@example.com", "Journey", "Learner");
        string applicationId;
        using (var applicationResponse = await SendAuthorizedAsync(
                   HttpMethod.Post,
                   "/api/v1/applications",
                   learnerToken,
                   new { courseOfferingId = offeringId }))
        {
            applicationResponse.EnsureSuccessStatusCode();
            using var application = JsonDocument.Parse(await applicationResponse.Content.ReadAsStringAsync());
            applicationId = application.RootElement.GetProperty("id").GetString()!;
            Assert.Equal("Pending", application.RootElement.GetProperty("status").GetString());
        }

        var adminToken = await RegisterAsync($"admin-{unique}@example.com", "Journey", "Admin");
        await AddRoleAsync($"admin-{unique}@example.com", RoleNames.Admin);
        adminToken = await LoginAsync($"admin-{unique}@example.com");

        using (var acceptanceResponse = await SendAuthorizedAsync(
                   HttpMethod.Post,
                   $"/api/v1/applications/{applicationId}/accept",
                   adminToken))
        {
            acceptanceResponse.EnsureSuccessStatusCode();
        }

        using (var paymentResponse = await SendAuthorizedAsync(
                   HttpMethod.Post,
                   "/api/v1/payments",
                   learnerToken,
                   new { applicationId }))
        {
            paymentResponse.EnsureSuccessStatusCode();
            using var payment = JsonDocument.Parse(await paymentResponse.Content.ReadAsStringAsync());
            Assert.True(payment.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Equal("Succeeded", payment.RootElement.GetProperty("payment").GetProperty("status").GetString());
            Assert.Equal("Active", payment.RootElement.GetProperty("enrollment").GetProperty("status").GetString());
        }

        using var enrollmentsResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/enrollments/me", learnerToken);
        enrollmentsResponse.EnsureSuccessStatusCode();
        using var enrollments = JsonDocument.Parse(await enrollmentsResponse.Content.ReadAsStringAsync());
        var enrollment = Assert.Single(enrollments.RootElement.EnumerateArray().ToArray());
        Assert.Equal(applicationId, enrollment.GetProperty("applicationId").GetString());
        Assert.Equal("Active", enrollment.GetProperty("status").GetString());
    }

    private async Task<Guid> SeedPublishedOfferingAsync(string unique)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new CourseCategory
        {
            Id = Guid.NewGuid(),
            Name = $"Acceptance category {unique}",
            Slug = $"acceptance-category-{unique}",
        };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = $"Acceptance-{unique}",
            Slug = $"acceptance-course-{unique}",
            Description = "Clean-database acceptance course.",
            Status = "Published",
            Type = "Offline",
            Price = 500m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = $"Acceptance Branch {unique}",
            RoomName = "Room 1",
            City = "Cairo",
            Address = "Acceptance test location",
            Capacity = 20,
            IsActive = true,
        };
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LocationId = location.Id,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
            EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(3),
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
        return offering.Id;
    }

    private async Task<string> RegisterAsync(string email, string firstName, string lastName)
    {
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
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> LoginAsync(string email)
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "P@ssw0rd123!",
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task AddRoleAsync(string email, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        var result = await userManager.AddToRoleAsync(user, role);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
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
}
