using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Identity;

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<UpdateProfileOutcome> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);
}
