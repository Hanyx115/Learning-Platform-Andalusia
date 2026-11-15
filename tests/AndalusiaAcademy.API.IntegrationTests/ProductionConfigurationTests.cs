using AndalusiaAcademy.API.Configuration;
using Microsoft.Extensions.Configuration;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ProductionConfigurationTests
{
    [Fact]
    public void ProductionConfiguration_RejectsMissingSecretsAndWildcardHosts()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*",
            ["Database:MigrateOnStartup"] = "true",
            ["CatalogSeed:Enabled"] = "true",
            ["HttpsRedirection:Enabled"] = "false",
            ["Authentication:RefreshCookie:Secure"] = "false",
            ["Authentication:RefreshCookie:SameSite"] = "Lax",
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, "Production"));

        Assert.Contains("DefaultConnection", exception.Message);
        Assert.Contains("Jwt:Key", exception.Message);
        Assert.Contains("AllowedHosts", exception.Message);
        Assert.Contains("Cors:AllowedOrigins", exception.Message);
        Assert.Contains("MigrateOnStartup", exception.Message);
        Assert.Contains("RefreshCookie:Secure", exception.Message);
        Assert.Contains("RefreshCookie:SameSite", exception.Message);
    }

    [Fact]
    public void ProductionConfiguration_AcceptsSecretsFromDeploymentConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=sql.example;Database=academy;User Id=academy;Password=from-secret-store;Encrypt=True",
            ["Jwt:Key"] = Convert.ToBase64String(new byte[32]),
            ["AllowedHosts"] = "academy.example.com;api.academy.example.com",
            ["Cors:AllowedOrigins:0"] = "https://academy.example.com",
            ["Database:MigrateOnStartup"] = "false",
            ["CatalogSeed:Enabled"] = "false",
            ["IdentitySeed:Enabled"] = "false",
            ["HttpsRedirection:Enabled"] = "true",
            ["Authentication:RefreshCookie:Secure"] = "true",
            ["Authentication:RefreshCookie:SameSite"] = "Strict",
        });

        ProductionConfigurationValidator.Validate(configuration, "Production");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
