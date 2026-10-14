using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

/// <summary>Named to avoid colliding with the project's own "Application" namespace.</summary>
public sealed class ApplicationSubmissionService(IApplicationRepository applicationRepository)
{
    public async Task<CreateApplicationOutcome> SubmitAsync(
        Guid userId,
        CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (await applicationRepository.HasPendingApplicationAsync(
            userId, request.CourseId, request.ProgramId, cancellationToken))
        {
            return CreateApplicationOutcome.Failure("You already have a pending application for this course or program.");
        }

        var application = await applicationRepository.AddAsync(
            userId, request.CourseId, request.ProgramId, cancellationToken);

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
