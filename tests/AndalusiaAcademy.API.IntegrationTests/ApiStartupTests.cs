using System.Net;
using System.Text.Json;

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
    public async Task OpenApiDocument_DescribesBearerAuthenticationOnProtectedOperations()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(responseStream);
        var root = document.RootElement;

        var bearerScheme = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearerScheme.GetProperty("bearerFormat").GetString());

        var profileOperation = root
            .GetProperty("paths")
            .GetProperty("/api/v1/profile")
            .GetProperty("get");

        var securityRequirement = Assert.Single(profileOperation.GetProperty("security").EnumerateArray());
        Assert.True(securityRequirement.TryGetProperty("Bearer", out _));

        var registerOperation = root
            .GetProperty("paths")
            .GetProperty("/api/v1/auth/register")
            .GetProperty("post");

        Assert.False(registerOperation.TryGetProperty("security", out _));
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

    [Fact]
    public async Task CorsPreflight_AllowsConfiguredFrontendOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/courses");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Contains("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin"));
    }
}
