using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Admin;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Identity.Services;

public sealed class ProfileService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IProfileService
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
        user.LearnerPetEnabled = request.LearnerPetEnabled ?? user.LearnerPetEnabled;
        user.LearnerPetType = request.LearnerPetType ?? user.LearnerPetType;

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

    public async Task<PaginatedResult<DirectoryUserDto>> GetUsersInRolePageAsync(
        string role,
        UserDirectoryQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedRole = role.Trim().ToUpperInvariant();
        var users =
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join identityRole in dbContext.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id
            where identityRole.NormalizedName == normalizedRole
            select user;

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(user =>
                user.FirstName.Contains(search) ||
                user.LastName.Contains(search) ||
                (user.FirstName + " " + user.LastName).Contains(search) ||
                (user.Email != null && user.Email.Contains(search)) ||
                (user.PhoneNumber != null && user.PhoneNumber.Contains(search)));
        }

        var status = query.Status?.Trim();
        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user => user.IsActive);
        }
        else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user => !user.IsActive);
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.PageSize;
        if (offset > int.MaxValue)
        {
            return new PaginatedResult<DirectoryUserDto>([], query.Page, query.PageSize, totalCount);
        }

        var items = await users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Id)
            .Skip((int)offset)
            .Take(query.PageSize)
            .Select(user => new DirectoryUserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.PhoneNumber!,
                user.IsActive))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<DirectoryUserDto>(items, query.Page, query.PageSize, totalCount);
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
            user.LearnerPetEnabled,
            user.LearnerPetType,
            roles.ToArray());
    }
}
