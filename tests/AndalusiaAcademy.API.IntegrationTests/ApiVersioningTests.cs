using System.Net;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ApiVersioningTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public void ApiVersioning_IsConfiguredForVersionOne()
    {
        var options = factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        Assert.Equal(new ApiVersion(1, 0), options.DefaultApiVersion);
        Assert.False(options.AssumeDefaultVersionWhenUnspecified);
        Assert.True(options.ReportApiVersions);
        Assert.IsType<UrlSegmentApiVersionReader>(options.ApiVersionReader);
    }

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

        Assert.Contains(
            "1.0",
            response.Headers.GetValues("api-supported-versions"));
    }

    [Fact]
    public async Task UnversionedRoute_ReturnsNotFound()
    {
        using var response = await _client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnsupportedVersionRoute_ReturnsNotFound()
    {
        using var response = await _client.GetAsync("/api/v2/ping");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
