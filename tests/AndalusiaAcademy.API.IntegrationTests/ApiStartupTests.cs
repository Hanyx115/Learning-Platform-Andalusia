using System.Net;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ApiStartupTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ApiHost_StartsSuccessfully()
    {
        using var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsAvailableInDevelopment()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SwaggerUi_IsAvailableInDevelopment()
    {
        using var response = await _client.GetAsync("/swagger/index.html");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        using var documentUrlsResponse = await _client.GetAsync("/swagger/documentUrls");
        documentUrlsResponse.EnsureSuccessStatusCode();

        var documentUrls = await documentUrlsResponse.Content.ReadAsStringAsync();
        Assert.Contains("/openapi/v1.json", documentUrls);
    }
}
