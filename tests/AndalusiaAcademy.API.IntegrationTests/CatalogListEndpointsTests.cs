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
    [InlineData("programs", "Beta", "Beta Program")]
    [InlineData("career-paths", "Beta", "Beta Career")]
    public async Task GetCatalogList_AppliesSearch(
        string endpoint,
        string search,
        string expectedTitle)
    {
        await factory.SeedProgramsAndCareerPathsAsync();

        using var response = await _client.GetAsync(
            $"/api/v1/{endpoint}?status=Published&search={search}&sort=title");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(expectedTitle, Assert.Single(root.GetProperty("items").EnumerateArray()).GetProperty("title").GetString());
        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("programs", "Gamma Program")]
    [InlineData("career-paths", "Gamma Career")]
    public async Task GetCatalogList_AppliesDescendingTitleSort(
        string endpoint,
        string expectedFirstTitle)
    {
        await factory.SeedProgramsAndCareerPathsAsync();

        using var response = await _client.GetAsync(
            $"/api/v1/{endpoint}?status=Published&sort=title-desc");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(
            expectedFirstTitle,
            document.RootElement.GetProperty("items")[0].GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("programs", "page=0")]
    [InlineData("programs", "pageSize=101")]
    [InlineData("career-paths", "page=0")]
    [InlineData("career-paths", "pageSize=101")]
    [InlineData("programs", "sort=unsupported")]
    [InlineData("career-paths", "sort=unsupported")]
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
