using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class AdminUserManagementEndpointsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_CanChangeRolesAndDeactivateUser_ExistingTokenAndLoginAreRejected()
    {
        var learner = await RegisterAsync();
        var admin = await RegisterAsAdminAsync();

        using var listResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/users", admin.Token);
        listResponse.EnsureSuccessStatusCode();
        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Contains(listDocument.RootElement.EnumerateArray(), user => user.GetProperty("id").GetGuid() == learner.UserId);

        using var rolesResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{learner.UserId}/roles",
            admin.Token,
            new { roles = new[] { RoleNames.Instructor, RoleNames.Manager } });
        rolesResponse.EnsureSuccessStatusCode();
        using var rolesDocument = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
        var roles = rolesDocument.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ToArray();
        Assert.Contains(RoleNames.Instructor, roles);
        Assert.Contains(RoleNames.Manager, roles);
        Assert.DoesNotContain(RoleNames.Learner, roles);

        using var statusResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{learner.UserId}/status",
            admin.Token,
            new { isActive = false });
        statusResponse.EnsureSuccessStatusCode();

        using var profileResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/profile", learner.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = learner.Email,
            password = learner.Password,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Learner_CannotManageUsers_AndAdminCannotDeactivateSelf()
    {
        var learner = await RegisterAsync();
        var admin = await RegisterAsAdminAsync();

        using var forbidden = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/users", learner.Token);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var conflict = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{admin.UserId}/status",
            admin.Token,
            new { isActive = false });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    private async Task<(string Token, string Email, string Password, Guid UserId)> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "AdminUser",
            lastName = "Test",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("accessToken").GetString()!;

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await userManager.FindByEmailAsync(email))!;
        return (token, email, password, user.Id);
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

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = registered.Email,
            password = registered.Password,
        });
        loginResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
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
