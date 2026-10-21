using AndalusiaAcademy.Application.DTOs.Progress;

namespace AndalusiaAcademy.Application.Repositories;

public interface IProgressRepository
{
    Task<bool> OfferingExistsAsync(Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    /// <summary>True if the user is assigned as instructor to at least one session within the offering.</summary>
    Task<bool> IsInstructorForOfferingAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OfferingProgressRecordDto>> GetForOfferingAsync(
        Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LearnerProgressDto>> GetForLearnerAsync(Guid userId, CancellationToken cancellationToken);
}
