using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.Application.Services;

/// <summary>Named to avoid colliding with the project's own "Application" namespace.</summary>
public sealed class ApplicationSubmissionService(
    IApplicationRepository applicationRepository,
    IOfferingRepository offeringRepository,
    INotificationOutbox notificationOutbox,
    TimeProvider timeProvider,
    ILogger<ApplicationSubmissionService> logger)
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

        if (await applicationRepository.HasOpenApplicationAsync(
            userId, request.CourseOfferingId, request.ProgramOfferingId, cancellationToken))
        {
            return CreateApplicationOutcome.Failure(
                "You already have a pending or accepted application for this offering.");
        }

        var application = await applicationRepository.AddAsync(
            userId, request.CourseOfferingId, request.ProgramOfferingId, cancellationToken);

        if (application is null)
        {
            return CreateApplicationOutcome.Failure(
                "You already have a pending or accepted application for this offering.");
        }

        await TryQueueNotificationAsync(
            new NotificationEventRequest(
                $"application-submitted:{application.Id:N}",
                NotificationTemplateCodes.ApplicationSubmitted,
                [userId],
                new Dictionary<string, string> { ["offering"] = offering.Title },
                "/profile/applications"),
            cancellationToken);

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

        if (updated is null)
        {
            return ReviewApplicationOutcome.Failure("Application not found or is no longer pending.");
        }

        await QueueStatusNotificationAsync(updated, cancellationToken);
        return ReviewApplicationOutcome.Success(updated);
    }

    public async Task<ReviewApplicationOutcome> RejectAsync(
        Guid applicationId,
        Guid reviewedByUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var updated = await applicationRepository.UpdateStatusIfPendingAsync(
            applicationId, ApplicationStatus.Rejected, reviewedByUserId, reason, cancellationToken);

        if (updated is null)
        {
            return ReviewApplicationOutcome.Failure("Application not found or is no longer pending.");
        }

        await QueueStatusNotificationAsync(updated, cancellationToken);
        return ReviewApplicationOutcome.Success(updated);
    }

    private Task QueueStatusNotificationAsync(ApplicationDto application, CancellationToken cancellationToken)
        => TryQueueNotificationAsync(
            new NotificationEventRequest(
                $"application-status:{application.Id:N}:{application.Status}",
                NotificationTemplateCodes.ApplicationStatusUpdated,
                [application.UserId],
                new Dictionary<string, string> { ["status"] = application.Status },
                "/profile/applications"),
            cancellationToken);

    private async Task TryQueueNotificationAsync(
        NotificationEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationOutbox.EnqueueAsync(request, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Could not queue application notification {EventKey}.", request.EventKey);
        }
    }
}
