using System.Net;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ApiVersioningTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task VersionedRoute_ReturnsSuccess()
    {
        using var response = await _client.GetAsync("/api/v1/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionedRoute_ReportsSupportedApiVersions()
    {
        using var response = await _client.GetAsync("/api/v1/ping");

        Assert.True(response.Headers.Contains("api-supported-versions"));
    }

    [Fact]
    public async Task UnversionedRoute_ReturnsNotFound()
    {
        using var response = await _client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
