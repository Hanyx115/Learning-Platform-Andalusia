using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.API.Configuration;
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
    public async Task Register_WithValidRequest_ReturnsAccessTokenAndProtectedRefreshCookie()
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
        Assert.False(root.TryGetProperty("refreshToken", out _));

        var setCookie = GetRefreshSetCookieHeader(response);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"path={RefreshTokenCookieOptions.CookiePath}", setCookie, StringComparison.OrdinalIgnoreCase);
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
        using var response = await _client.PostAsync("/api/v1/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidRefreshToken_RevokesItInStorage()
    {
        var session = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken) },
        };

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenHash = scope.ServiceProvider.GetRequiredService<ITokenService>().Hash(session.RefreshCookie);
        var storedToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash);
        Assert.NotNull(storedToken.RevokedAtUtc);
    }

    [Fact]
    public async Task ForgotPassword_ForKnownAndUnknownEmail_BothReturnGenericSuccess()
    {
        var email = (await RegisterAsync()).Email;

        using var knownResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        using var unknownResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new { email = $"{Guid.NewGuid()}@example.com" });

        Assert.True(
            knownResponse.StatusCode == HttpStatusCode.OK,
            $"Expected OK but received {knownResponse.StatusCode}: {await knownResponse.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);

        using var knownDocument = JsonDocument.Parse(await knownResponse.Content.ReadAsStringAsync());
        using var unknownDocument = JsonDocument.Parse(await unknownResponse.Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrWhiteSpace(knownDocument.RootElement.GetProperty("resetToken").GetString()));
        Assert.Equal(JsonValueKind.Null, unknownDocument.RootElement.GetProperty("resetToken").ValueKind);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPasswordOnly()
    {
        var email = (await RegisterAsync()).Email;

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
    public async Task Refresh_WithValidToken_ReturnsNewTokensAndRevokesOldOne()
    {
        var session = await RegisterAsync();

        using var response = await _client.PostAsync("/api/v1/auth/refresh", content: null);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(document.RootElement.TryGetProperty("refreshToken", out _));
        var newRefreshToken = GetRefreshCookieValue(response);
        Assert.NotEqual(session.RefreshCookie, newRefreshToken);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenHash = scope.ServiceProvider.GetRequiredService<ITokenService>().Hash(session.RefreshCookie);
        var oldToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash);
        Assert.NotNull(oldToken.RevokedAtUtc);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        using var request = CreateCookieRequest(
            "/api/v1/auth/refresh",
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithAlreadyRotatedToken_ReturnsUnauthorizedAndRevokesWholeChain()
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
        var session = await RegisterAsync(client);

        using var firstRequest = CreateCookieRequest("/api/v1/auth/refresh", session.RefreshCookie);
        using var firstRefresh = await client.SendAsync(firstRequest);
        firstRefresh.EnsureSuccessStatusCode();
        var rotatedRefreshToken = GetRefreshCookieValue(firstRefresh);

        // Reusing the original (now-revoked) token simulates a stolen token being replayed.
        using var reuseRequest = CreateCookieRequest("/api/v1/auth/refresh", session.RefreshCookie);
        using var reuseResponse = await client.SendAsync(reuseRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // The whole chain, including the legitimate rotated token, must now be dead too.
        using var rotatedRequest = CreateCookieRequest("/api/v1/auth/refresh", rotatedRefreshToken);
        using var rotatedTokenResponse = await client.SendAsync(rotatedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, rotatedTokenResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var email = (await RegisterAsync()).Email;

        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new { email, token = "not-a-real-token", newPassword = "N3wP@ssword123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<RegisteredSession> RegisterAsync(HttpClient? client = null)
    {
        client ??= _client;
        var email = $"{Guid.NewGuid()}@example.com";

        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
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
        return new RegisteredSession(
            root.GetProperty("accessToken").GetString()!,
            GetRefreshCookieValue(response),
            email);
    }

    private static HttpRequestMessage CreateCookieRequest(string path, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"{RefreshTokenCookieOptions.DefaultName}={refreshToken}");
        return request;
    }

    private static string GetRefreshCookieValue(HttpResponseMessage response)
    {
        var setCookie = GetRefreshSetCookieHeader(response);
        var cookiePair = setCookie.Split(';', 2)[0];
        return Uri.UnescapeDataString(cookiePair[(cookiePair.IndexOf('=') + 1)..].Trim('"'));
    }

    private static string GetRefreshSetCookieHeader(HttpResponseMessage response) =>
        Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(
                $"{RefreshTokenCookieOptions.DefaultName}=",
                StringComparison.Ordinal));

    private sealed record RegisteredSession(string AccessToken, string RefreshCookie, string Email);
}
