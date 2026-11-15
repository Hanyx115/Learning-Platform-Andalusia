using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using AndalusiaAcademy.Infrastructure.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CmsAndNotificationsEndpointsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PublicFaqs_ReturnPublishedBilingualContent_WithoutAuthentication()
    {
        using var response = await _client.GetAsync("/api/v1/faqs");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetArrayLength() >= 5);
        var first = document.RootElement.EnumerateArray().First();
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("titleEn").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("titleAr").GetString()));
    }

    [Fact]
    public async Task CmsContent_RequiresContentManager_AndCanCreatePublishedContent()
    {
        using var anonymous = await _client.GetAsync("/api/v1/cms/content");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var learner = await RegisterAsync();
        using var forbidden = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/cms/content", learner.Token);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var contentManager = await RegisterAsAsync(RoleNames.ContentManager);
        var key = $"integration-{Guid.NewGuid():N}";
        using var created = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/cms/content", contentManager.Token, new
        {
            contentType = CmsContentTypes.Faq,
            key,
            titleEn = "Integration question",
            titleAr = "سؤال تكامل",
            bodyEn = "Integration answer.",
            bodyAr = "إجابة التكامل.",
            imageUrl = (string?)null,
            linkUrl = (string?)null,
            relatedUserId = (Guid?)null,
            displayOrder = 999,
            isPublished = true,
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var publicFaqs = await _client.GetAsync("/api/v1/faqs");
        publicFaqs.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await publicFaqs.Content.ReadAsStringAsync());
        Assert.Contains(document.RootElement.EnumerateArray(), item => item.GetProperty("key").GetString() == key);
    }

    [Fact]
    public async Task Register_QueuesAccountNotification_AndNotificationEndpointsEnforceRoles()
    {
        var learner = await RegisterAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(await dbContext.OutboxMessages.AnyAsync(message =>
                message.EventType == NotificationTemplateCodes.AccountCreated &&
                message.PayloadJson.Contains(learner.UserId.ToString())));
        }

        var processor = factory.Services.GetRequiredService<NotificationOutboxProcessor>();
        Assert.True(await DrainOutboxAsync(processor) > 0);

        using var mine = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/notifications", learner.Token);
        mine.EnsureSuccessStatusCode();
        using (var mineDocument = JsonDocument.Parse(await mine.Content.ReadAsStringAsync()))
        {
            Assert.Contains(
                mineDocument.RootElement.GetProperty("items").EnumerateArray(),
                item => item.GetProperty("eventType").GetString() == NotificationTemplateCodes.AccountCreated);
        }

        using var forbiddenTemplates = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/notifications/templates", learner.Token);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenTemplates.StatusCode);

        var contentManager = await RegisterAsAsync(RoleNames.ContentManager);
        using var templates = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/notifications/templates", contentManager.Token);
        templates.EnsureSuccessStatusCode();
        using var templateDocument = JsonDocument.Parse(await templates.Content.ReadAsStringAsync());
        Assert.True(templateDocument.RootElement.GetArrayLength() >= 10);

        using var announcement = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/notifications/announcements", contentManager.Token, new
        {
            recipientUserIds = new[] { learner.UserId },
            titleEn = "Integration announcement",
            titleAr = "إعلان تكامل",
            messageEn = "This is an integration announcement.",
            messageAr = "هذا إعلان تكامل.",
            linkUrl = "/profile/notifications",
        });
        Assert.Equal(HttpStatusCode.Accepted, announcement.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await verificationDbContext.OutboxMessages.AnyAsync(message =>
            message.EventType == NotificationTemplateCodes.CourseAnnouncement &&
            message.PayloadJson.Contains(learner.UserId.ToString())));

        Assert.True(await processor.ProcessOnceAsync(CancellationToken.None) > 0);
        using var updatedMine = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/notifications", learner.Token);
        updatedMine.EnsureSuccessStatusCode();
        using var updatedDocument = JsonDocument.Parse(await updatedMine.Content.ReadAsStringAsync());
        Assert.Contains(
            updatedDocument.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("eventType").GetString() == NotificationTemplateCodes.CourseAnnouncement);
    }

    private static async Task<int> DrainOutboxAsync(NotificationOutboxProcessor processor)
    {
        var processed = 0;
        for (var batch = 0; batch < 20; batch++)
        {
            var count = await processor.ProcessOnceAsync(CancellationToken.None);
            processed += count;
            if (count == 0)
            {
                break;
            }
        }

        return processed;
    }

    private async Task<(string Token, string Email, string Password, Guid UserId)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "CMS",
            lastName = "Test",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await userManager.FindByEmailAsync(email))!;
        return (document.RootElement.GetProperty("accessToken").GetString()!, email, password, user.Id);
    }

    private async Task<(string Token, Guid UserId)> RegisterAsAsync(string role)
    {
        var registered = await RegisterAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByIdAsync(registered.UserId.ToString()))!;
            await userManager.AddToRoleAsync(user, role);
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

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string url,
        string token,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }
}
