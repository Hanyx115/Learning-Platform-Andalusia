using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class AuthEndpointsTests(AuthApiWebApplicationFactory factory) : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidRequest_ReturnsTokens()
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
        var root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokens()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "Learner",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

        loginResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "P@ssw0rd123!",
            firstName = "Test",
            lastName = "Learner",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "WrongPassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = $"{Guid.NewGuid()}@example.com", password = "P@ssw0rd123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingFields_ReturnsValidationProblem()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class AuthApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AuthApiTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        // The InMemory provider never runs migrations, so the roles seeded via HasData need EnsureCreated to materialize.
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();

        return host;
    }
}
