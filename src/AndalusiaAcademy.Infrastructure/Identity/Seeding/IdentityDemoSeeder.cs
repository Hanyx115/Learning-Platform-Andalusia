using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace AndalusiaAcademy.Infrastructure.Identity.Seeding;

public static class IdentityDemoSeeder
{
    private const string DemoPassword = "Demo@12345!";

    private static readonly (string Email, string FirstName, string LastName, string PhoneNumber, string Role)[] DemoUsers =
    [
        ("admin.demo@andalusia.test", "Admin", "Demo", "+201000000001", RoleNames.Admin),
        ("instructor.demo@andalusia.test", "Instructor", "Demo", "+201000000002", RoleNames.Instructor),
        ("learner.demo@andalusia.test", "Learner", "Demo", "+201000000003", RoleNames.Learner),
    ];

    public static async Task<int> SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken = default)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var createRoleResult = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed role '{roleName}': {string.Join(", ", createRoleResult.Errors.Select(error => error.Description))}");
            }
        }

        var addedCount = 0;

        foreach (var (email, firstName, lastName, phoneNumber, role) in DemoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                CountryCode = "EG",
                PreferredLanguage = "en",
                ConsentedAt = DateTimeOffset.UtcNow,
                EmailConfirmed = true,
            };

            var createUserResult = await userManager.CreateAsync(user, DemoPassword);
            if (!createUserResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed demo user '{email}': {string.Join(", ", createUserResult.Errors.Select(error => error.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
            addedCount++;
        }

        return addedCount;
    }
}
