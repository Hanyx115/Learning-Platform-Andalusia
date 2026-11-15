using AndalusiaAcademy.Application.DTOs.Applications;

namespace AndalusiaAcademy.Application.Repositories;

public interface IOfferingRepository
{
    Task<IReadOnlyList<OfferingDto>> GetOpenCourseOfferingsAsync(
        string courseSlug,
        DateTimeOffset availableAtUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OfferingDto>> GetOpenProgramOfferingsAsync(
        string programSlug,
        DateTimeOffset availableAtUtc,
        CancellationToken cancellationToken);

    Task<OfferingTargetDto?> GetCourseOfferingAsync(Guid offeringId, CancellationToken cancellationToken);

    Task<OfferingTargetDto?> GetProgramOfferingAsync(Guid offeringId, CancellationToken cancellationToken);
}
