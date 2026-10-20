using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;

namespace AndalusiaAcademy.Application.Repositories;

public interface IApplicationRepository
{
    Task<bool> HasPendingApplicationAsync(
        Guid userId,
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken);

    Task<ApplicationDto> AddAsync(
        Guid userId,
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ApplicationDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<ApplicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PaginatedResult<ApplicationDto>> GetPageAsync(
        ApplicationListQuery query,
        CancellationToken cancellationToken);

    /// <summary>Updates the application's status only if it is still Pending (optimistic guard against double-review); returns null if the update did not apply.</summary>
    Task<ApplicationDto?> UpdateStatusIfPendingAsync(
        Guid id,
        string newStatus,
        Guid reviewedByUserId,
        string? rejectionReason,
        CancellationToken cancellationToken);
}
