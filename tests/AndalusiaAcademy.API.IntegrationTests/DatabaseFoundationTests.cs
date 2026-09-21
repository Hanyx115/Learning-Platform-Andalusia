using AndalusiaAcademy.Infrastructure;
using AndalusiaAcademy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class DatabaseFoundationTests
{
    private const string TestConnectionString =
        "Server=localhost;Database=AndalusiaAcademyTests;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void AddInfrastructure_RegistersSqlServerDbContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Fact]
    public void ApplicationDbContext_ContainsInitialFoundationMigration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        using var context = new ApplicationDbContext(options);

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.EndsWith("_InitialDatabaseFoundation", StringComparison.Ordinal));
    }
}
