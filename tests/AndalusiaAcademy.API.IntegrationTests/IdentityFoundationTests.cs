using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class IdentityFoundationTests
{
    private const string TestConnectionString =
        "Server=localhost;Database=AndalusiaAcademyIdentityTests;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void AddInfrastructure_RegistersIdentityManagersAndSecureDefaults()
    {
        using var provider = CreateServiceProvider();

        Assert.NotNull(provider.GetService<UserManager<ApplicationUser>>());
        Assert.NotNull(provider.GetService<RoleManager<ApplicationRole>>());

        var identityOptions = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        Assert.Equal(8, identityOptions.Password.RequiredLength);
        Assert.Equal(4, identityOptions.Password.RequiredUniqueChars);
        Assert.True(identityOptions.Password.RequireDigit);
        Assert.True(identityOptions.Password.RequireLowercase);
        Assert.True(identityOptions.Password.RequireNonAlphanumeric);
        Assert.True(identityOptions.Password.RequireUppercase);
        // Off until a real email provider exists (decided 2026-10-13); revisit when notifications/email are built.
        Assert.False(identityOptions.SignIn.RequireConfirmedEmail);
        Assert.True(identityOptions.User.RequireUniqueEmail);
        Assert.Equal(5, identityOptions.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), identityOptions.Lockout.DefaultLockoutTimeSpan);

        var tokenOptions = provider
            .GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>()
            .Value;
        Assert.Equal(TimeSpan.FromHours(2), tokenOptions.TokenLifespan);
    }

    [Fact]
    public void ApplicationDbContext_MapsRequiredIdentityFieldsAndInitialRoles()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        using var context = new ApplicationDbContext(options);
        var user = context.Model.FindEntityType(typeof(ApplicationUser));
        var role = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(ApplicationRole));

        Assert.NotNull(user);
        Assert.Equal("AspNetUsers", user.GetTableName());
        Assert.False(user.FindProperty(nameof(ApplicationUser.FirstName))!.IsNullable);
        Assert.Equal(100, user.FindProperty(nameof(ApplicationUser.FirstName))!.GetMaxLength());
        Assert.False(user.FindProperty(nameof(ApplicationUser.Email))!.IsNullable);
        Assert.False(user.FindProperty(nameof(ApplicationUser.PhoneNumber))!.IsNullable);
        Assert.Equal(2, user.FindProperty(nameof(ApplicationUser.CountryCode))!.GetMaxLength());
        Assert.Equal(5, user.FindProperty(nameof(ApplicationUser.PreferredLanguage))!.GetMaxLength());

        Assert.NotNull(role);
        var seededRoleNames = role.GetSeedData()
            .Select(values => values[nameof(ApplicationRole.Name)] as string)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            RoleNames.All.OrderBy(name => name, StringComparer.Ordinal),
            seededRoleNames);
    }

    [Fact]
    public void ApplicationDbContext_ContainsIdentityFoundationMigration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        using var context = new ApplicationDbContext(options);

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.EndsWith("_AddIdentityFoundation", StringComparison.Ordinal));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
