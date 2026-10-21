using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace AndalusiaAcademy.Infrastructure.Identity.Services;

public sealed class ProfileService(UserManager<ApplicationUser> userManager) : IProfileService
{
    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<UpdateProfileOutcome> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UpdateProfileOutcome.Failure(["User not found."]);
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.CountryCode = request.CountryCode;
        user.PreferredLanguage = request.PreferredLanguage;

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return UpdateProfileOutcome.Failure(updateResult.Errors.Select(error => error.Description).ToArray());
        }

        return UpdateProfileOutcome.Success(await ToDtoAsync(user));
    }

    public async Task<IReadOnlyList<ProfileDto>> GetUsersInRoleAsync(string role, CancellationToken cancellationToken)
    {
        var users = await userManager.GetUsersInRoleAsync(role);
        var profiles = new List<ProfileDto>(users.Count);

        foreach (var user in users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName))
        {
            profiles.Add(await ToDtoAsync(user));
        }

        return profiles;
    }

    private async Task<ProfileDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new ProfileDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber!,
            user.CountryCode,
            user.PreferredLanguage,
            roles.ToArray());
    }
}
