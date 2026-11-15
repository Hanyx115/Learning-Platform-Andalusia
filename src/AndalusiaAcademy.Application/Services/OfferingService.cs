using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class OfferingService(IOfferingRepository offeringRepository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<OfferingDto>> GetOpenCourseOfferingsAsync(
        string courseSlug,
        CancellationToken cancellationToken)
        => offeringRepository.GetOpenCourseOfferingsAsync(courseSlug, timeProvider.GetUtcNow(), cancellationToken);

    public Task<IReadOnlyList<OfferingDto>> GetOpenProgramOfferingsAsync(
        string programSlug,
        CancellationToken cancellationToken)
        => offeringRepository.GetOpenProgramOfferingsAsync(programSlug, timeProvider.GetUtcNow(), cancellationToken);
}
