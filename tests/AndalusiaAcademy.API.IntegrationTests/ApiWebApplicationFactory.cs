using Microsoft.AspNetCore.DataProtection;
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

    private readonly string _courseMaterialStoragePath = Path.Combine(
        Path.GetTempPath(),
        "andalusia-course-material-tests",
        Guid.NewGuid().ToString("N"));

    private readonly string _dataProtectionKeysPath = Path.Combine(
        Path.GetTempPath(),
        "andalusia-data-protection-tests",
        Guid.NewGuid().ToString("N"));

    internal const string TestJwtKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    internal const string TestConnectionStringEnvironmentVariable =
        "ANDALUSIA_TEST_CONNECTION_STRING";

    public int LoginRateLimitPermitLimit { get; set; } = 10_000;

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

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = testConnectionString,
                ["Jwt:Key"] = TestJwtKey,
                ["CatalogSeed:Enabled"] = "false",
                ["IdentitySeed:Enabled"] = "false",
                ["Database:MigrateOnStartup"] = "true",
                ["Notifications:ProcessingEnabled"] = "false",
                ["RateLimiting:Authentication:LoginPermitLimit"] =
                    LoginRateLimitPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["AssignmentSubmissions:StoragePath"] = _assignmentSubmissionStoragePath,
                ["CourseMaterials:StoragePath"] = _courseMaterialStoragePath,
            }));
        builder.ConfigureServices(services =>
        {
            Directory.CreateDirectory(_dataProtectionKeysPath);
            services
                .AddDataProtection()
                .SetApplicationName("AndalusiaAcademy.IntegrationTests")
                .PersistKeysToFileSystem(new DirectoryInfo(_dataProtectionKeysPath));
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

        if (disposing && Directory.Exists(_courseMaterialStoragePath))
        {
            Directory.Delete(_courseMaterialStoragePath, recursive: true);
        }

        if (disposing && Directory.Exists(_dataProtectionKeysPath))
        {
            Directory.Delete(_dataProtectionKeysPath, recursive: true);
        }
    }
}
