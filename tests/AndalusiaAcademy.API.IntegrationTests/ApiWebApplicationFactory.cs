using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _assignmentSubmissionStoragePath = Path.Combine(
        Path.GetTempPath(),
        "andalusia-assignment-submission-tests",
        Guid.NewGuid().ToString("N"));

    internal const string TestJwtKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    internal const string TestConnectionStringEnvironmentVariable =
        "ANDALUSIA_TEST_CONNECTION_STRING";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var testConnectionString = Environment.GetEnvironmentVariable(
            TestConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(testConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {TestConnectionStringEnvironmentVariable} to an isolated SQL Server database before running API integration tests. " +
                "Use scripts/run-sprint4-tests.ps1 for the supported Docker-based workflow.");
        }

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = testConnectionString,
                ["Jwt:Key"] = TestJwtKey,
                ["CatalogSeed:Enabled"] = "false",
                ["IdentitySeed:Enabled"] = "false",
                ["Database:MigrateOnStartup"] = "true",
                ["AssignmentSubmissions:StoragePath"] = _assignmentSubmissionStoragePath,
            }));
        builder.ConfigureServices(services =>
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(ThrowingController).Assembly);
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_assignmentSubmissionStoragePath))
        {
            Directory.Delete(_assignmentSubmissionStoragePath, recursive: true);
        }
    }
}
