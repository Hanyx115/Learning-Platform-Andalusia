using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

/// <summary>Named to avoid colliding with the project's own "Application" namespace.</summary>
public sealed class ApplicationSubmissionService(
    IApplicationRepository applicationRepository,
    IOfferingRepository offeringRepository,
    TimeProvider timeProvider)
{
    public async Task<CreateApplicationOutcome> SubmitAsync(
        Guid userId,
        CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var offering = request.CourseOfferingId.HasValue
            ? await offeringRepository.GetCourseOfferingAsync(request.CourseOfferingId.Value, cancellationToken)
            : await offeringRepository.GetProgramOfferingAsync(request.ProgramOfferingId!.Value, cancellationToken);

        if (offering is null)
        {
            return CreateApplicationOutcome.Failure("The selected offering was not found.");
        }

        var now = timeProvider.GetUtcNow();
        if (offering.Status != AndalusiaAcademy.Domain.Models.OfferingStatuses.Open ||
            offering.ApplicationOpensAtUtc > now ||
            offering.ApplicationClosesAtUtc < now)
        {
            return CreateApplicationOutcome.Failure("The selected offering is not open for applications.");
        }

        if (await applicationRepository.HasPendingApplicationAsync(
            userId, request.CourseOfferingId, request.ProgramOfferingId, cancellationToken))
        {
            return CreateApplicationOutcome.Failure("You already have a pending application for this offering.");
        }

        var application = await applicationRepository.AddAsync(
            userId, request.CourseOfferingId, request.ProgramOfferingId, cancellationToken);

        return CreateApplicationOutcome.Success(application);
    }

    public Task<IReadOnlyList<ApplicationDto>> GetMyApplicationsAsync(Guid userId, CancellationToken cancellationToken)
        => applicationRepository.GetForUserAsync(userId, cancellationToken);

    public Task<PaginatedResult<ApplicationDto>> GetPageAsync(
        ApplicationListQuery query,
        CancellationToken cancellationToken)
        => applicationRepository.GetPageAsync(query, cancellationToken);

    public async Task<ReviewApplicationOutcome> AcceptAsync(
        Guid applicationId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken)
    {
        var updated = await applicationRepository.UpdateStatusIfPendingAsync(
            applicationId, ApplicationStatus.Accepted, reviewedByUserId, rejectionReason: null, cancellationToken);

        return updated is null
            ? ReviewApplicationOutcome.Failure("Application not found or is no longer pending.")
            : ReviewApplicationOutcome.Success(updated);
    }

    public async Task<ReviewApplicationOutcome> RejectAsync(
        Guid applicationId,
        Guid reviewedByUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var updated = await applicationRepository.UpdateStatusIfPendingAsync(
            applicationId, ApplicationStatus.Rejected, reviewedByUserId, reason, cancellationToken);

        return updated is null
            ? ReviewApplicationOutcome.Failure("Application not found or is no longer pending.")
            : ReviewApplicationOutcome.Success(updated);
    }
}
