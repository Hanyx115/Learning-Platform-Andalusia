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

public sealed class CompensationEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task InstructorCoursePayout_RequiresManagerReviewAndAdminApproval_ThenCreditsPrivateBalance()
    {
        var instructor = await RegisterAsAsync(RoleNames.Instructor, "Course", "Instructor");
        var manager = await RegisterAsAsync(RoleNames.Manager, "Course", "Manager");
        var admin = await RegisterAsAsync(RoleNames.Admin, "Course", "Admin");
        var otherUser = await RegisterAsAsync(RoleNames.Learner, "Other", "Learner");
        var offeringId = await SeedCompletedCourseAsync(instructor.UserId);

        using var targetsResponse = await SendAsync(HttpMethod.Get, "/api/v1/compensation/instructor-targets", manager.Token);
        targetsResponse.EnsureSuccessStatusCode();
        using var targetsDocument = JsonDocument.Parse(await targetsResponse.Content.ReadAsStringAsync());
        var target = targetsDocument.RootElement.EnumerateArray().Single(item => item.GetProperty("courseOfferingId").GetGuid() == offeringId);
        Assert.True(target.GetProperty("isEligibleForPayout").GetBoolean());

        using var setRate = await SendAsync(HttpMethod.Put, "/api/v1/compensation/instructor-rates", manager.Token, new
        {
            instructorUserId = instructor.UserId,
            courseOfferingId = offeringId,
            amount = 1250m,
            currency = "EGP",
        });
        Assert.Equal(HttpStatusCode.NoContent, setRate.StatusCode);

        using var refreshedTargets = await SendAsync(HttpMethod.Get, "/api/v1/compensation/instructor-targets", manager.Token);
        refreshedTargets.EnsureSuccessStatusCode();
        using var refreshedDocument = JsonDocument.Parse(await refreshedTargets.Content.ReadAsStringAsync());
        var ratedTarget = refreshedDocument.RootElement.EnumerateArray().Single(item => item.GetProperty("courseOfferingId").GetGuid() == offeringId);
        var rateId = ratedTarget.GetProperty("rateId").GetGuid();

        using var createPayout = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/instructor-rates/{rateId}/payout", manager.Token);
        createPayout.EnsureSuccessStatusCode();
        using var payoutDocument = JsonDocument.Parse(await createPayout.Content.ReadAsStringAsync());
        var payoutId = payoutDocument.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Draft", payoutDocument.RootElement.GetProperty("status").GetString());

        using var managerApproval = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/approve", manager.Token);
        Assert.Equal(HttpStatusCode.Forbidden, managerApproval.StatusCode);
        using var adminReview = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/review", admin.Token);
        Assert.Equal(HttpStatusCode.Forbidden, adminReview.StatusCode);

        using var review = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/review", manager.Token);
        review.EnsureSuccessStatusCode();
        using var approve = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/approve", admin.Token);
        approve.EnsureSuccessStatusCode();

        Assert.Equal(1250m, await GetBalanceAsync(instructor.Token));
        Assert.Equal(0m, await GetBalanceAsync(otherUser.Token));
    }

    [Fact]
    public async Task MonthlyManagerPayout_UsesAdminRate_AndLearnerCanOnlyCreditOwnDemoBalance()
    {
        var paidManager = await RegisterAsAsync(RoleNames.Manager, "Paid", "Manager");
        var reviewingManager = await RegisterAsAsync(RoleNames.Manager, "Reviewing", "Manager");
        var admin = await RegisterAsAsync(RoleNames.Admin, "Monthly", "Admin");
        var learner = await RegisterAsAsync(RoleNames.Learner, "Balance", "Learner");

        using var setRate = await SendAsync(HttpMethod.Put, $"/api/v1/compensation/staff-rates/{paidManager.UserId}", admin.Token, new
        {
            amount = 5000m,
            currency = "EGP",
        });
        Assert.Equal(HttpStatusCode.NoContent, setRate.StatusCode);

        var now = DateTimeOffset.UtcNow;
        using var create = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/compensation/staff-rates/{paidManager.UserId}/payout",
            reviewingManager.Token,
            new { year = now.Year, month = now.Month });
        create.EnsureSuccessStatusCode();
        using var createdDocument = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var payoutId = createdDocument.RootElement.GetProperty("id").GetGuid();

        using var selfReview = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/review", paidManager.Token);
        Assert.Equal(HttpStatusCode.Conflict, selfReview.StatusCode);
        using var review = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/review", reviewingManager.Token);
        review.EnsureSuccessStatusCode();
        using var approve = await SendAsync(HttpMethod.Post, $"/api/v1/compensation/payouts/{payoutId}/approve", admin.Token);
        approve.EnsureSuccessStatusCode();
        Assert.Equal(5000m, await GetBalanceAsync(paidManager.Token));

        using var demoCredit = await SendAsync(HttpMethod.Post, "/api/v1/balance/me/demo-credit", learner.Token, new
        {
            amount = 300m,
            method = "Fawry",
        });
        demoCredit.EnsureSuccessStatusCode();
        Assert.Equal(300m, await GetBalanceAsync(learner.Token));
        Assert.Equal(5000m, await GetBalanceAsync(paidManager.Token));
    }

    private async Task<Guid> SeedCompletedCourseAsync(Guid instructorUserId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CourseCategory { Id = Guid.NewGuid(), Name = $"Compensation {suffix}", Slug = $"compensation-{suffix}" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Title = $"Completed course {suffix}",
            Slug = $"completed-{suffix}", Description = "Compensation test", Status = "Published",
            Type = "Offline", CreatedAt = DateTimeOffset.UtcNow.AddMonths(-4),
        };
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(), CourseId = course.Id,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMonths(-2), EndsAtUtc = DateTimeOffset.UtcNow.AddMonths(-1),
            ApplicationOpensAtUtc = DateTimeOffset.UtcNow.AddMonths(-4),
            ApplicationClosesAtUtc = DateTimeOffset.UtcNow.AddMonths(-3),
            Price = 500m, Currency = "EGP", Capacity = 20, Status = OfferingStatuses.Closed,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-4),
        };
        var session = new CourseSession
        {
            Id = Guid.NewGuid(), CourseOfferingId = offering.Id, InstructorUserId = instructorUserId,
            Title = "Completed session", StartsAtUtc = offering.StartsAtUtc.AddDays(1),
            EndsAtUtc = offering.StartsAtUtc.AddDays(1).AddHours(2), Status = "Scheduled",
        };
        dbContext.AddRange(category, course, offering, session);
        await dbContext.SaveChangesAsync();
        return offering.Id;
    }

    private async Task<decimal> GetBalanceAsync(string token)
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/balance/me", token);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("balance").GetDecimal();
    }

    private async Task<(string Token, Guid UserId)> RegisterAsAsync(string role, string firstName, string lastName)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";
        using var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password, firstName, lastName, phoneNumber = "+201234567890",
            countryCode = "EG", preferredLanguage = "en",
        });
        register.EnsureSuccessStatusCode();

        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByEmailAsync(email))!;
            if (role != RoleNames.Learner)
            {
                await userManager.AddToRoleAsync(user, role);
                await userManager.RemoveFromRoleAsync(user, RoleNames.Learner);
            }
            userId = user.Id;
        }

        using var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return (document.RootElement.GetProperty("accessToken").GetString()!, userId);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await _client.SendAsync(request);
    }
}
