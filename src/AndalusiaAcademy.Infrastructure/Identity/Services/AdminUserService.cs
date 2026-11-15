using AndalusiaAcademy.Application.DTOs.Admin;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Identity.Services;

public sealed class AdminUserService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokenRepository) : IAdminUserService
{
    private static readonly HashSet<string> ManageableRoles = new(
        [RoleNames.Learner, RoleNames.Instructor, RoleNames.ContentManager, RoleNames.Manager],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> PrivilegedRoles = new(
        [RoleNames.Admin],
        StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                Email = user.Email!,
                user.FirstName,
                user.LastName,
                PhoneNumber = user.PhoneNumber!,
                user.IsActive,
            })
            .ToListAsync(cancellationToken);

        var roleAssignments = await dbContext.UserRoles
            .AsNoTracking()
            .Join(
                dbContext.Roles.AsNoTracking(),
                assignment => assignment.RoleId,
                role => role.Id,
                (assignment, role) => new { assignment.UserId, Role = role.Name! })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleAssignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(assignment => assignment.Role)
                    .OrderBy(role => role)
                    .ToArray());

        return users
            .Select(user => new AdminUserDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.IsActive,
                rolesByUser.GetValueOrDefault(user.Id) ?? []))
            .ToArray();
    }

    public async Task<AdminUserUpdateOutcome> SetActiveAsync(
        Guid actingAdminUserId,
        Guid targetUserId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
        {
            return AdminUserUpdateOutcome.Fail(AdminUserUpdateFailure.NotFound, "User not found.");
        }

        if (actingAdminUserId == targetUserId && !isActive)
        {
            return AdminUserUpdateOutcome.Fail(
                AdminUserUpdateFailure.Conflict,
                "You cannot deactivate your own administrator account.");
        }

        user.IsActive = isActive;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return AdminUserUpdateOutcome.Fail(
                AdminUserUpdateFailure.InvalidRequest,
                string.Join(" ", updateResult.Errors.Select(error => error.Description)));
        }

        if (!isActive)
        {
            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        }

        return AdminUserUpdateOutcome.Success(await ToDtoAsync(user));
    }

    public async Task<AdminUserUpdateOutcome> UpdateRolesAsync(
        Guid targetUserId,
        IReadOnlyCollection<string> requestedRoles,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
        {
            return AdminUserUpdateOutcome.Fail(AdminUserUpdateFailure.NotFound, "User not found.");
        }

        var canonicalRequestedRoles = requestedRoles
            .Select(role => RoleNames.All.FirstOrDefault(known =>
                string.Equals(known, role?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (canonicalRequestedRoles.Any(role => role is null) ||
            canonicalRequestedRoles.Any(role => !ManageableRoles.Contains(role!)))
        {
            return AdminUserUpdateOutcome.Fail(
                AdminUserUpdateFailure.InvalidRequest,
                $"Roles can only be managed from: {string.Join(", ", ManageableRoles.OrderBy(role => role))}.");
        }

        var desiredManageableRoles = canonicalRequestedRoles
            .Select(role => role!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var currentRoles = await userManager.GetRolesAsync(user);
        var preservedPrivilegedRoles = currentRoles.Where(PrivilegedRoles.Contains).ToArray();
        if (desiredManageableRoles.Length == 0 && preservedPrivilegedRoles.Length == 0)
        {
            return AdminUserUpdateOutcome.Fail(
                AdminUserUpdateFailure.InvalidRequest,
                "Every active account must keep at least one role.");
        }

        var manageableCurrentRoles = currentRoles.Where(ManageableRoles.Contains).ToArray();
        var rolesToRemove = manageableCurrentRoles
            .Except(desiredManageableRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolesToAdd = desiredManageableRoles
            .Except(manageableCurrentRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return AdminUserUpdateOutcome.Fail(
                    AdminUserUpdateFailure.InvalidRequest,
                    string.Join(" ", removeResult.Errors.Select(error => error.Description)));
            }
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return AdminUserUpdateOutcome.Fail(
                    AdminUserUpdateFailure.InvalidRequest,
                    string.Join(" ", addResult.Errors.Select(error => error.Description)));
            }
        }

        return AdminUserUpdateOutcome.Success(await ToDtoAsync(user));
    }

    private async Task<AdminUserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AdminUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber!,
            user.IsActive,
            roles.OrderBy(role => role).ToArray());
    }
}
