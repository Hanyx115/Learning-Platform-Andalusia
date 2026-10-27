using AndalusiaAcademy.Application.DTOs.Admin;

namespace AndalusiaAcademy.Application.Identity;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<AdminUserUpdateOutcome> SetActiveAsync(
        Guid actingAdminUserId,
        Guid targetUserId,
        bool isActive,
        CancellationToken cancellationToken);

    Task<AdminUserUpdateOutcome> UpdateRolesAsync(
        Guid targetUserId,
        IReadOnlyCollection<string> requestedRoles,
        CancellationToken cancellationToken);
}
