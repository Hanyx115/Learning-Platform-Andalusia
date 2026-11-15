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

public sealed class AdminPaymentManagementEndpointsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_CanFailPendingPayment_AndAuditHistoryIsReturned()
    {
        var learner = await RegisterAsync();
        var admin = await RegisterAsAdminAsync();
        var paymentId = await SeedPendingPaymentAsync(learner.UserId);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/payments/{paymentId}/fail",
            admin.Token);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(PaymentStatus.Failed, document.RootElement.GetProperty("status").GetString());
        var history = document.RootElement.GetProperty("statusHistory").EnumerateArray().ToArray();
        Assert.Equal(2, history.Length);
        Assert.StartsWith("Admin:", history[^1].GetProperty("source").GetString());
        Assert.Equal(PaymentStatus.Failed, history[^1].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Learner_CannotFailPayment_AndCompletedPaymentCannotBeChanged()
    {
        var learner = await RegisterAsync();
        var admin = await RegisterAsAdminAsync();
        var pendingPaymentId = await SeedPendingPaymentAsync(learner.UserId);

        using var forbidden = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/payments/{pendingPaymentId}/fail",
            learner.Token);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var succeededPaymentId = await SeedPendingPaymentAsync(learner.UserId, PaymentStatus.Succeeded);
        using var conflict = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/payments/{succeededPaymentId}/fail",
            admin.Token);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    private async Task<Guid> SeedPendingPaymentAsync(Guid userId, string status = PaymentStatus.Pending)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Payment Cat {suffix}", Slug = $"payment-cat-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Payment Course {suffix}",
            Slug = $"payment-course-{suffix}", Description = "Test", Status = "Published", Type = "Online",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(), CourseId = course.Id, StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(2),
            EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(3), ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(1), Price = 100, Currency = "EGP",
            Capacity = 10, Status = OfferingStatuses.Open, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var application = new LearnerApplication
        {
            Id = Guid.NewGuid(), UserId = userId, CourseOfferingId = offering.Id,
            Status = ApplicationStatus.Accepted, SubmittedAt = DateTimeOffset.UtcNow,
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, UserId = userId, Amount = 100,
            Currency = "EGP", Status = status, IdempotencyKey = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = status == PaymentStatus.Succeeded ? DateTimeOffset.UtcNow : null,
        };
        dbContext.AddRange(category, course, offering, application, payment, new PaymentStatusHistory
        {
            Id = Guid.NewGuid(), PaymentId = payment.Id, Status = status,
            ChangedAt = DateTimeOffset.UtcNow, Source = "Test",
        });
        await dbContext.SaveChangesAsync();
        return payment.Id;
    }

    private async Task<(string Token, Guid UserId, string Email, string Password)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password, firstName = "Payment", lastName = "User", phoneNumber = "+201234567890",
            countryCode = "EG", preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await userManager.FindByEmailAsync(email))!;
        return (document.RootElement.GetProperty("accessToken").GetString()!, user.Id, email, password);
    }

    private async Task<(string Token, Guid UserId)> RegisterAsAdminAsync()
    {
        var registered = await RegisterAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByIdAsync(registered.UserId.ToString()))!;
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
            await userManager.RemoveFromRoleAsync(user, RoleNames.Learner);
        }
        using var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = registered.Email,
            password = registered.Password,
        });
        login.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return (document.RootElement.GetProperty("accessToken").GetString()!, registered.UserId);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
