using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using AndalusiaAcademy.API.Configuration;
using AndalusiaAcademy.API.Controllers.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class AuthRateLimitingTests
{
    [Theory]
    [InlineData(nameof(AuthController.Register), AuthRateLimitPolicies.Registration)]
    [InlineData(nameof(AuthController.Login), AuthRateLimitPolicies.Login)]
    [InlineData(nameof(AuthController.Refresh), AuthRateLimitPolicies.Session)]
    [InlineData(nameof(AuthController.Logout), AuthRateLimitPolicies.Session)]
    [InlineData(nameof(AuthController.ForgotPassword), AuthRateLimitPolicies.Recovery)]
    [InlineData(nameof(AuthController.ResetPassword), AuthRateLimitPolicies.Recovery)]
    public void AuthenticationAction_UsesExpectedRateLimitPolicy(string actionName, string policyName)
    {
        var action = typeof(AuthController).GetMethod(actionName);
        var attribute = action?.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(policyName, attribute.PolicyName);
    }

    [Fact]
    public void ProductionDefaults_AreRestrictive()
    {
        var options = new AuthRateLimitOptions();

        Assert.InRange(options.LoginPermitLimit, 1, 10);
        Assert.InRange(options.RegistrationPermitLimit, 1, 10);
        Assert.InRange(options.RecoveryPermitLimit, 1, 5);
        Assert.InRange(options.SessionPermitLimit, 1, 60);
    }

    [Fact]
    public async Task Login_AboveConfiguredLimit_ReturnsProblemDetailsWithRetryAfter()
    {
        await using var factory = new ApiWebApplicationFactory
        {
            LoginRateLimitPermitLimit = 2,
        };
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var allowedResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = $"missing-{Guid.NewGuid():N}@example.com",
                password = "P@ssw0rd123!",
            });
            Assert.Equal(HttpStatusCode.Unauthorized, allowedResponse.StatusCode);
        }

        using var rejectedResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@example.com",
            password = "P@ssw0rd123!",
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.True(rejectedResponse.Headers.Contains("Retry-After"));
        Assert.Equal("application/problem+json", rejectedResponse.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await rejectedResponse.Content.ReadAsStringAsync());
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }
}
