using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AndalusiaAcademy.API;

/// <summary>
/// Creates the model for EF tooling without starting the web host or requiring local secrets.
/// The connection is not opened while migrations are generated.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=AndalusiaAcademyDesign;Trusted_Connection=True;TrustServerCertificate=True",
                sqlServer => sqlServer.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
