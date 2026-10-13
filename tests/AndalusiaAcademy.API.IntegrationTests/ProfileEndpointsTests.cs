using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ProfileEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var response = await _client.GetAsync("/api/v1/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsOwnProfile()
    {
        var (accessToken, email) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(email, root.GetProperty("email").GetString());
        Assert.Equal("Test", root.GetProperty("firstName").GetString());
        Assert.Contains("Learner", root.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task UpdateProfile_WithValidRequest_PersistsChangesAndReturnsUpdatedProfile()
    {
        var (accessToken, _) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new
            {
                firstName = "Updated",
                lastName = "Name",
                phoneNumber = "+201111111111",
                countryCode = "SA",
                preferredLanguage = "ar",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Updated", root.GetProperty("firstName").GetString());
        Assert.Equal("Name", root.GetProperty("lastName").GetString());
        Assert.Equal("SA", root.GetProperty("countryCode").GetString());
        Assert.Equal("ar", root.GetProperty("preferredLanguage").GetString());

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profile");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var getResponse = await _client.SendAsync(getRequest);
        using var getDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Updated", getDocument.RootElement.GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithMissingFields_ReturnsValidationProblem()
    {
        var (accessToken, _) = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new { firstName = "", lastName = "", phoneNumber = "", countryCode = "", preferredLanguage = "" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(string AccessToken, string Email)> RegisterAsync()
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
        return (document.RootElement.GetProperty("accessToken").GetString()!, email);
    }
}
