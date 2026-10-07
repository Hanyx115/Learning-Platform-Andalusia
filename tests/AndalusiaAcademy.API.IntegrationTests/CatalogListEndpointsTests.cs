using System.Net;
using System.Text.Json;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CatalogListEndpointsTests(CatalogApiWebApplicationFactory factory)
    : IClassFixture<CatalogApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("programs", "Beta Program")]
    [InlineData("career-paths", "Beta Career")]
    public async Task GetCatalogList_ReturnsPublishedPaginatedResponse(
        string endpoint,
        string expectedTitle)
    {
        await factory.SeedProgramsAndCareerPathsAsync();

        using var response = await _client.GetAsync(
            $"/api/v1/{endpoint}?status=Published&page=2&pageSize=1");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(expectedTitle, root.GetProperty("items")[0].GetProperty("title").GetString());
        Assert.Equal(2, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, root.GetProperty("totalPages").GetInt32());
    }

    [Theory]
    [InlineData("programs", "page=0")]
    [InlineData("programs", "pageSize=101")]
    [InlineData("career-paths", "page=0")]
    [InlineData("career-paths", "pageSize=101")]
    public async Task GetCatalogList_WithInvalidPaging_ReturnsBadRequest(
        string endpoint,
        string query)
    {
        using var response = await _client.GetAsync($"/api/v1/{endpoint}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("programs")]
    [InlineData("career-paths")]
    public async Task GetCatalogList_WithPageBeyondIntegerOffset_ReturnsEmptyPage(string endpoint)
    {
        await factory.SeedProgramsAndCareerPathsAsync();

        using var response = await _client.GetAsync(
            $"/api/v1/{endpoint}?status=Published&page={int.MaxValue}&pageSize=100");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Empty(root.GetProperty("items").EnumerateArray());
        Assert.Equal(3, root.GetProperty("totalCount").GetInt32());
    }
}
