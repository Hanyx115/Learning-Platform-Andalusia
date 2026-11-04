using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ProfileEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var response = await _client.GetAsync("/api/v1/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsOwnProfile()
    {
        var (accessToken, email) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(email, root.GetProperty("email").GetString());
        Assert.Equal("Test", root.GetProperty("firstName").GetString());
        Assert.Contains("Learner", root.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task UpdateProfile_WithValidRequest_PersistsChangesAndReturnsUpdatedProfile()
    {
        var (accessToken, _) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new
            {
                firstName = "Updated",
                lastName = "Name",
                phoneNumber = "+201111111111",
                countryCode = "SA",
                preferredLanguage = "ar",
                learnerPetEnabled = true,
                learnerPetType = "owl",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Updated", root.GetProperty("firstName").GetString());
        Assert.Equal("Name", root.GetProperty("lastName").GetString());
        Assert.Equal("SA", root.GetProperty("countryCode").GetString());
        Assert.Equal("ar", root.GetProperty("preferredLanguage").GetString());
        Assert.True(root.GetProperty("learnerPetEnabled").GetBoolean());
        Assert.Equal("owl", root.GetProperty("learnerPetType").GetString());

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profile");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var getResponse = await _client.SendAsync(getRequest);
        using var getDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Updated", getDocument.RootElement.GetProperty("firstName").GetString());
        Assert.True(getDocument.RootElement.GetProperty("learnerPetEnabled").GetBoolean());
        Assert.Equal("owl", getDocument.RootElement.GetProperty("learnerPetType").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithMissingFields_ReturnsValidationProblem()
    {
        var (accessToken, _) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new { firstName = "", lastName = "", phoneNumber = "", countryCode = "", preferredLanguage = "" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProfileByUserId_AsLearner_ReturnsForbidden()
    {
        var (accessToken, _) = await RegisterAsync();
        var (_, otherUserId, _) = await RegisterWithIdAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/profile/{otherUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProfileByUserId_AsAdmin_ReturnsTargetUserProfile()
    {
        var (adminAccessToken, _) = await RegisterAsAsync(RoleNames.Admin);
        var (_, targetUserId, targetEmail) = await RegisterWithIdAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/profile/{targetUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(targetEmail, document.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetProfileByUserId_AsManager_ReturnsTargetUserProfile()
    {
        var (managerAccessToken, _) = await RegisterAsAsync(RoleNames.Manager);
        var (_, targetUserId, targetEmail) = await RegisterWithIdAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/profile/{targetUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerAccessToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(targetEmail, document.RootElement.GetProperty("email").GetString());
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync()
    {
        var (accessToken, _, email) = await RegisterWithIdAsync();
        return (accessToken, email);
    }

    private async Task<(string AccessToken, Guid UserId, string Email)> RegisterWithIdAsync()
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
        var accessToken = document.RootElement.GetProperty("accessToken").GetString()!;

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userId = (await userManager.FindByEmailAsync(email))!.Id;

        return (accessToken, userId, email);
    }

    /// <summary>Registers a user, promotes them to <paramref name="role"/> in the database, then logs in
    /// again so the returned access token carries the new role claim (the register-time token only has Learner).</summary>
    private async Task<(string AccessToken, string Email)> RegisterAsAsync(string role)
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
        return (document.RootElement.GetProperty("accessToken").GetString()!, email);
    }
}
