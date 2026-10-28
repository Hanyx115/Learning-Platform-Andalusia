using AndalusiaAcademy.Application.DTOs.Payments;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.Application.Services;

public sealed class PaymentService(
    IPaymentRepository paymentRepository,
    IInstructorRepository instructorRepository,
    INotificationOutbox notificationOutbox,
    ILogger<PaymentService> logger)
{
    public async Task<PayForApplicationOutcome> PayAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var outcome = await paymentRepository.PayForApplicationAsync(applicationId, userId, cancellationToken);
        if (!outcome.Succeeded || outcome.Payment is null || outcome.Enrollment is null)
        {
            return outcome;
        }

        var offering = outcome.Enrollment.CourseTitle ?? outcome.Enrollment.ProgramTitle ?? "your offering";
        await TryQueueNotificationAsync(
            new NotificationEventRequest(
                $"payment-confirmed:{outcome.Payment.Id:N}",
                NotificationTemplateCodes.PaymentConfirmed,
                [userId],
                new Dictionary<string, string>
                {
                    ["amount"] = outcome.Payment.Amount.ToString("0.##"),
                    ["currency"] = outcome.Payment.Currency,
                },
                "/profile/payments"),
            cancellationToken);
        await TryQueueNotificationAsync(
            new NotificationEventRequest(
                $"enrollment-confirmed:{outcome.Enrollment.Id:N}",
                NotificationTemplateCodes.EnrollmentConfirmed,
                [userId],
                new Dictionary<string, string> { ["offering"] = offering },
                "/profile/enrollments"),
            cancellationToken);

        var instructorIds = await instructorRepository.GetInstructorUserIdsForOfferingAsync(
            outcome.Enrollment.CourseOfferingId,
            outcome.Enrollment.ProgramOfferingId,
            cancellationToken);
        if (instructorIds.Count > 0)
        {
            await TryQueueNotificationAsync(
                new NotificationEventRequest(
                    $"new-learner-enrollment:{outcome.Enrollment.Id:N}",
                    NotificationTemplateCodes.NewLearnerEnrollment,
                    instructorIds,
                    new Dictionary<string, string>
                    {
                        ["learnerName"] = "A learner",
                        ["offering"] = offering,
                    },
                    "/instructor/students"),
                cancellationToken);
        }

        return outcome;
    }

    public Task<IReadOnlyList<PaymentDto>> GetMyPaymentsAsync(Guid userId, CancellationToken cancellationToken)
        => paymentRepository.GetForUserAsync(userId, cancellationToken);

    public Task<IReadOnlyList<AdminPaymentDto>> GetAllAsync(CancellationToken cancellationToken)
        => paymentRepository.GetAllAsync(cancellationToken);

    public Task<AdminPaymentActionOutcome> FailPendingAsync(
        Guid paymentId,
        Guid actingAdminUserId,
        CancellationToken cancellationToken)
        => paymentRepository.FailPendingAsync(paymentId, actingAdminUserId, cancellationToken);

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
            logger.LogError(exception, "Could not queue payment notification {EventKey}.", request.EventKey);
        }
    }
}
