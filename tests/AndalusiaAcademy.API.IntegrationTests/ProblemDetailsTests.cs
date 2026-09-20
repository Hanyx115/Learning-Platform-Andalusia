using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ProblemDetailsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UnhandledException_ReturnsProblemDetails()
    {
        using var response = await _client.GetAsync("/api/v1/test-errors");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem!.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal(
            "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1",
            problem.Type);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task RouteNotFound_ReturnsProblemDetails()
    {
        using var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }
}
