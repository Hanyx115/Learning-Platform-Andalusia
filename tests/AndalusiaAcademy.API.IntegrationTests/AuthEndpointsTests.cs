using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

// Uses the shared, real-SQL-Server-backed ApiWebApplicationFactory (not an InMemory one): reset-password's
// RevokeAllForUserAsync uses ExecuteUpdateAsync, which the EF InMemory provider does not support.
public sealed class AuthEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidRequest_ReturnsTokens()
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
        var root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokens()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "Learner",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

        loginResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "P@ssw0rd123!",
            firstName = "Test",
            lastName = "Learner",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "WrongPassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = $"{Guid.NewGuid()}@example.com", password = "P@ssw0rd123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingFields_ReturnsValidationProblem()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = "anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidRefreshToken_RevokesItInStorage()
    {
        var (accessToken, refreshToken) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken }),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenHash = scope.ServiceProvider.GetRequiredService<ITokenService>().Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash);
        Assert.NotNull(storedToken.RevokedAtUtc);
    }

    [Fact]
    public async Task ForgotPassword_ForKnownAndUnknownEmail_BothReturnGenericSuccess()
    {
        var (_, _, email) = await RegisterWithEmailAsync();

        using var knownResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        using var unknownResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new { email = $"{Guid.NewGuid()}@example.com" });

        Assert.Equal(HttpStatusCode.OK, knownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);

        using var knownDocument = JsonDocument.Parse(await knownResponse.Content.ReadAsStringAsync());
        using var unknownDocument = JsonDocument.Parse(await unknownResponse.Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrWhiteSpace(knownDocument.RootElement.GetProperty("resetToken").GetString()));
        Assert.Equal(JsonValueKind.Null, unknownDocument.RootElement.GetProperty("resetToken").ValueKind);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPasswordOnly()
    {
        var (_, _, email) = await RegisterWithEmailAsync();

        using var forgotResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        using var forgotDocument = JsonDocument.Parse(await forgotResponse.Content.ReadAsStringAsync());
        var resetToken = forgotDocument.RootElement.GetProperty("resetToken").GetString();

        const string newPassword = "N3wP@ssword123!";
        using var resetResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new { email, token = resetToken, newPassword });
        resetResponse.EnsureSuccessStatusCode();

        using var oldLoginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "P@ssw0rd123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        using var newLoginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = newPassword });
        newLoginResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var (_, _, email) = await RegisterWithEmailAsync();

        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new { email, token = "not-a-real-token", newPassword = "N3wP@ssword123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(string AccessToken, string RefreshToken)> RegisterAsync()
    {
        var (accessToken, refreshToken, _) = await RegisterWithEmailAsync();
        return (accessToken, refreshToken);
    }

    private async Task<(string AccessToken, string RefreshToken, string Email)> RegisterWithEmailAsync()
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
        var root = document.RootElement;
        return (root.GetProperty("accessToken").GetString()!, root.GetProperty("refreshToken").GetString()!, email);
    }
}
