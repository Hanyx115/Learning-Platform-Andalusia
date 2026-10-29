using System.Net;
using System.Text.Json;
using AndalusiaAcademy.API.Middleware;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class HealthCheckTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthyResponse()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Liveness_ReturnsStructuredResponseAndCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "health-check-123");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("health-check-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("self", document.RootElement.GetProperty("checks")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Readiness_ReportsDatabaseAndNotificationOutbox()
    {
        using var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var checks = document.RootElement.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Contains(checks, check => check.GetProperty("name").GetString() == "database");
        Assert.Contains(checks, check => check.GetProperty("name").GetString() == "notification-outbox");
    }
}
