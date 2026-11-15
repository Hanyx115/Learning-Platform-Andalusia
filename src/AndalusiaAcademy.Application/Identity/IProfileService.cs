using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Admin;

namespace AndalusiaAcademy.Application.Identity;

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<UpdateProfileOutcome> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProfileDto>> GetUsersInRoleAsync(string role, CancellationToken cancellationToken);

    Task<PaginatedResult<DirectoryUserDto>> GetUsersInRolePageAsync(
        string role,
        UserDirectoryQuery query,
        CancellationToken cancellationToken);
}
