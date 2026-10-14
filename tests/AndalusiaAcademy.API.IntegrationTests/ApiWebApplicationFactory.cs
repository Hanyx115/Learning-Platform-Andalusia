using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string TestJwtKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
            }));
        builder.ConfigureServices(services =>
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(ThrowingController).Assembly);
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
