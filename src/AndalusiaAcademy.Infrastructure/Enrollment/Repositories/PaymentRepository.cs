using System.Data;
using AndalusiaAcademy.Application.DTOs.Enrollments;
using AndalusiaAcademy.Application.DTOs.Payments;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Repositories;

public sealed class PaymentRepository(ApplicationDbContext dbContext, IPaymentProcessor paymentProcessor)
    : IPaymentRepository
{
    private const string Source = "Simulated";
    private const string AdminSource = "Admin";

    public async Task<PayForApplicationOutcome> PayForApplicationAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.LearnerApplications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Status,
                a.CourseOfferingId,
                CourseId = a.CourseOffering != null ? a.CourseOffering.CourseId : (Guid?)null,
                CourseTitle = a.CourseOffering != null ? a.CourseOffering.Course!.Title : null,
                CoursePrice = a.CourseOffering != null ? a.CourseOffering.Price : (decimal?)null,
                OfferingStatus = a.CourseOffering != null
                    ? a.CourseOffering.Status
                    : a.ProgramOffering!.Status,
                OfferingCapacity = a.CourseOffering != null
                    ? a.CourseOffering.Capacity
                    : a.ProgramOffering!.Capacity,
                a.ProgramOfferingId,
                ProgramId = a.ProgramOffering != null ? a.ProgramOffering.ProgramId : (Guid?)null,
                ProgramTitle = a.ProgramOffering != null ? a.ProgramOffering.Program!.Title : null,
                ProgramPrice = a.ProgramOffering != null ? a.ProgramOffering.Price : (decimal?)null,
                StartsAtUtc = a.CourseOffering != null
                    ? a.CourseOffering.StartsAtUtc
                    : a.ProgramOffering!.StartsAtUtc,
                EndsAtUtc = a.CourseOffering != null
                    ? a.CourseOffering.EndsAtUtc
                    : a.ProgramOffering!.EndsAtUtc,
                LocationId = a.CourseOffering != null
                    ? a.CourseOffering.LocationId
                    : a.ProgramOffering!.LocationId,
                LocationBranchName = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? null : a.CourseOffering.Location.BranchName)
                    : (a.ProgramOffering!.Location == null ? null : a.ProgramOffering.Location.BranchName),
                LocationRoomName = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? null : a.CourseOffering.Location.RoomName)
                    : (a.ProgramOffering!.Location == null ? null : a.ProgramOffering.Location.RoomName),
                LocationCity = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? null : a.CourseOffering.Location.City)
                    : (a.ProgramOffering!.Location == null ? null : a.ProgramOffering.Location.City),
                LocationAddress = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? null : a.CourseOffering.Location.Address)
                    : (a.ProgramOffering!.Location == null ? null : a.ProgramOffering.Location.Address),
                LocationMapUrl = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? null : a.CourseOffering.Location.MapUrl)
                    : (a.ProgramOffering!.Location == null ? null : a.ProgramOffering.Location.MapUrl),
                LocationCapacity = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? (int?)null : a.CourseOffering.Location.Capacity)
                    : (a.ProgramOffering!.Location == null ? (int?)null : a.ProgramOffering.Location.Capacity),
                LocationIsActive = a.CourseOffering != null
                    ? (a.CourseOffering.Location == null ? (bool?)null : a.CourseOffering.Location.IsActive)
                    : (a.ProgramOffering!.Location == null ? (bool?)null : a.ProgramOffering.Location.IsActive),
                Currency = a.CourseOffering != null
                    ? a.CourseOffering.Currency
                    : a.ProgramOffering!.Currency,
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Same "not found" message whether it doesn't exist or belongs to someone else, so this
        // endpoint can't be used to probe other learners' application ids.
        if (application is null || application.UserId != userId)
        {
            return PayForApplicationOutcome.Failure("Application not found.");
        }

        if (application.Status != ApplicationStatus.Accepted)
        {
            return PayForApplicationOutcome.Failure("This application must be accepted before it can be paid for.");
        }

        var now = DateTimeOffset.UtcNow;
        if (application.OfferingStatus == OfferingStatuses.Cancelled)
        {
            return PayForApplicationOutcome.Failure("This offering has been cancelled and can no longer be paid for.");
        }

        if (application.EndsAtUtc <= now)
        {
            return PayForApplicationOutcome.Failure("This offering has already ended and can no longer be paid for.");
        }

        if (application.LocationId.HasValue && application.LocationIsActive != true)
        {
            return PayForApplicationOutcome.Failure("This offering's training location is not active.");
        }

        var amount = application.CourseOfferingId.HasValue ? application.CoursePrice : application.ProgramPrice;

        if (amount is null)
        {
            return PayForApplicationOutcome.Failure("This course or program does not have a price set yet.");
        }

        var paymentId = Guid.NewGuid();
        var pendingHistoryId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        Payment? payment = null;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        // Reserve capacity and the application payment slot atomically. Pending payments count as
        // temporary reservations so two simultaneous checkouts cannot oversell the same offering.
        var reservationError = await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var reservationTransaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var currentOffering = await dbContext.LearnerApplications
                .AsNoTracking()
                .Where(candidate => candidate.Id == applicationId && candidate.Status == ApplicationStatus.Accepted)
                .Select(candidate => new
                {
                    Status = candidate.CourseOfferingId != null
                        ? candidate.CourseOffering!.Status
                        : candidate.ProgramOffering!.Status,
                    Capacity = candidate.CourseOfferingId != null
                        ? candidate.CourseOffering!.Capacity
                        : candidate.ProgramOffering!.Capacity,
                    EndsAtUtc = candidate.CourseOfferingId != null
                        ? candidate.CourseOffering!.EndsAtUtc
                        : candidate.ProgramOffering!.EndsAtUtc,
                    LocationIsActive = candidate.CourseOfferingId != null
                        ? (candidate.CourseOffering!.LocationId == null || candidate.CourseOffering.Location!.IsActive)
                        : (candidate.ProgramOffering!.LocationId == null || candidate.ProgramOffering.Location!.IsActive),
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (currentOffering is null)
            {
                return "This application is no longer accepted.";
            }
            if (currentOffering.Status == OfferingStatuses.Cancelled || currentOffering.EndsAtUtc <= now)
            {
                return "This offering is no longer available for payment.";
            }
            if (!currentOffering.LocationIsActive)
            {
                return "This offering's training location is not active.";
            }

            var hasActivePayment = await dbContext.Payments
                .AsNoTracking()
                .AnyAsync(candidate =>
                    candidate.ApplicationId == applicationId && candidate.Status != PaymentStatus.Failed,
                    cancellationToken);
            if (hasActivePayment)
            {
                return "A payment already exists for this application.";
            }

            var alreadyEnrolled = await dbContext.Enrollments
                .AsNoTracking()
                .AnyAsync(enrollment =>
                        enrollment.UserId == userId &&
                        enrollment.Status == EnrollmentStatus.Active &&
                        enrollment.CourseOfferingId == application.CourseOfferingId &&
                        enrollment.ProgramOfferingId == application.ProgramOfferingId,
                    cancellationToken);
            if (alreadyEnrolled)
            {
                return "You are already enrolled in this offering.";
            }

            int activeEnrollments;
            int pendingReservations;
            if (application.CourseOfferingId.HasValue)
            {
                var offeringId = application.CourseOfferingId.Value;
                activeEnrollments = await dbContext.Enrollments.AsNoTracking().CountAsync(enrollment =>
                    enrollment.CourseOfferingId == offeringId && enrollment.Status == EnrollmentStatus.Active,
                    cancellationToken);
                pendingReservations = await dbContext.Payments.AsNoTracking().CountAsync(candidate =>
                    candidate.Status == PaymentStatus.Pending &&
                    candidate.Application!.CourseOfferingId == offeringId,
                    cancellationToken);
            }
            else
            {
                var offeringId = application.ProgramOfferingId!.Value;
                activeEnrollments = await dbContext.Enrollments.AsNoTracking().CountAsync(enrollment =>
                    enrollment.ProgramOfferingId == offeringId && enrollment.Status == EnrollmentStatus.Active,
                    cancellationToken);
                pendingReservations = await dbContext.Payments.AsNoTracking().CountAsync(candidate =>
                    candidate.Status == PaymentStatus.Pending &&
                    candidate.Application!.ProgramOfferingId == offeringId,
                    cancellationToken);
            }

            if (activeEnrollments + pendingReservations >= currentOffering.Capacity)
            {
                return "This offering is full. No seats are available.";
            }

            payment = new Payment
            {
                Id = paymentId,
                ApplicationId = applicationId,
                UserId = userId,
                Amount = amount.Value,
                Currency = application.Currency,
                Status = PaymentStatus.Pending,
                IdempotencyKey = idempotencyKey,
                CreatedAt = now,
            };
            dbContext.Payments.Add(payment);
            dbContext.PaymentStatusHistories.Add(new PaymentStatusHistory
            {
                Id = pendingHistoryId,
                PaymentId = payment.Id,
                Status = PaymentStatus.Pending,
                ChangedAt = now,
                Source = Source,
            });

            // Persisted before calling the processor so a crash mid-call leaves a recoverable
            // Pending row instead of losing the payment attempt entirely.
            await dbContext.SaveChangesAsync(cancellationToken);
            await reservationTransaction.CommitAsync(cancellationToken);
            return null;
        });

        if (reservationError is not null)
        {
            return PayForApplicationOutcome.Failure(reservationError);
        }

        payment ??= await dbContext.Payments.SingleAsync(candidate => candidate.Id == paymentId, cancellationToken);

        var result = await paymentProcessor.ProcessAsync(payment.Id, payment.Amount, payment.Currency, cancellationToken);
        var completedAt = DateTimeOffset.UtcNow;

        if (!result.Succeeded)
        {
            payment.Status = PaymentStatus.Failed;
            dbContext.PaymentStatusHistories.Add(new PaymentStatusHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Status = PaymentStatus.Failed,
                ChangedAt = completedAt,
                Source = Source,
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            return PayForApplicationOutcome.Failure(result.FailureReason ?? "Payment failed.");
        }

        // The offering may have been cancelled or its physical location deactivated while an
        // external processor was completing. Never convert that successful attempt into an invalid
        // enrollment. The simulated processor does not capture real funds; a real provider must
        // refund this branch as part of its webhook/reconciliation implementation.
        var canStillEnroll = await dbContext.LearnerApplications
            .AsNoTracking()
            .Where(candidate => candidate.Id == applicationId)
            .AnyAsync(candidate =>
                candidate.CourseOfferingId != null
                    ? candidate.CourseOffering!.Status != OfferingStatuses.Cancelled &&
                      candidate.CourseOffering.EndsAtUtc > completedAt &&
                      (candidate.CourseOffering.LocationId == null || candidate.CourseOffering.Location!.IsActive)
                    : candidate.ProgramOffering!.Status != OfferingStatuses.Cancelled &&
                      candidate.ProgramOffering.EndsAtUtc > completedAt &&
                      (candidate.ProgramOffering.LocationId == null || candidate.ProgramOffering.Location!.IsActive),
                cancellationToken);

        if (!canStillEnroll)
        {
            payment.Status = PaymentStatus.Failed;
            dbContext.PaymentStatusHistories.Add(new PaymentStatusHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Status = PaymentStatus.Failed,
                ChangedAt = completedAt,
                Source = Source,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return PayForApplicationOutcome.Failure(
                "The offering became unavailable before enrollment could be completed.");
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.CompletedAt = completedAt;
        dbContext.PaymentStatusHistories.Add(new PaymentStatusHistory
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Status = PaymentStatus.Succeeded,
            ChangedAt = completedAt,
            Source = Source,
        });

        var enrollment = new LearnerEnrollment
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            PaymentId = payment.Id,
            UserId = userId,
            CourseOfferingId = application.CourseOfferingId,
            ProgramOfferingId = application.ProgramOfferingId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = completedAt,
        };
        dbContext.Enrollments.Add(enrollment);
        dbContext.Entry(enrollment).Property<bool>("IntegrityProtected").CurrentValue = true;

        // Payment success and Enrollment creation commit in this one SaveChangesAsync call, so the
        // two can never be left out of sync with each other.
        await dbContext.SaveChangesAsync(cancellationToken);

        var paymentDto = new PaymentDto(
            payment.Id, payment.ApplicationId, payment.Amount, payment.Currency, payment.Status,
            payment.CreatedAt, payment.CompletedAt,
            [
                new PaymentStatusHistoryDto(Guid.Empty, PaymentStatus.Pending, payment.CreatedAt, Source),
                new PaymentStatusHistoryDto(Guid.Empty, PaymentStatus.Succeeded, completedAt, Source),
            ]);

        var enrollmentDto = new EnrollmentDto(
            enrollment.Id, enrollment.ApplicationId, enrollment.PaymentId,
            enrollment.CourseOfferingId,
            application.CourseId, application.CourseTitle,
            enrollment.ProgramOfferingId,
            application.ProgramId, application.ProgramTitle,
            application.StartsAtUtc, application.EndsAtUtc,
            application.LocationId.HasValue
                ? new TrainingLocationDto(
                    application.LocationId.Value,
                    application.LocationBranchName!,
                    application.LocationRoomName!,
                    application.LocationCity!,
                    application.LocationAddress!,
                    application.LocationMapUrl,
                    application.LocationCapacity!.Value,
                    application.LocationIsActive!.Value)
                : null,
            enrollment.Status, enrollment.EnrolledAt, enrollment.CompletedAt, enrollment.CancelledAt);

        return PayForApplicationOutcome.Success(paymentDto, enrollmentDto);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.UserId == userId)
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new PaymentDto(
                payment.Id, payment.ApplicationId, payment.Amount, payment.Currency, payment.Status,
                payment.CreatedAt, payment.CompletedAt,
                payment.StatusHistory
                    .OrderBy(history => history.ChangedAt)
                    .Select(history => new PaymentStatusHistoryDto(
                        history.Id, history.Status, history.ChangedAt, history.Source))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminPaymentDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var payments = await dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new AdminPaymentRow(
                payment.Id,
                payment.UserId,
                dbContext.Users.Where(user => user.Id == payment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                dbContext.Users.Where(user => user.Id == payment.UserId)
                    .Select(user => user.Email!).Single(),
                payment.ApplicationId,
                payment.Application != null && payment.Application.CourseOffering != null
                    ? payment.Application.CourseOffering.Course!.Title
                    : null,
                payment.Application != null && payment.Application.ProgramOffering != null
                    ? payment.Application.ProgramOffering.Program!.Title
                    : null,
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.CreatedAt,
                payment.CompletedAt))
            .ToListAsync(cancellationToken);

        return await AddStatusHistoryAsync(payments, cancellationToken);
    }

    public async Task<AdminPaymentActionOutcome> FailPendingAsync(
        Guid paymentId,
        Guid actingAdminUserId,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(candidate => candidate.Id == paymentId, cancellationToken);

        if (payment is null)
        {
            return AdminPaymentActionOutcome.Fail(AdminPaymentActionFailure.NotFound, "Payment not found.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return AdminPaymentActionOutcome.Fail(
                AdminPaymentActionFailure.Conflict,
                "Only a pending payment can be marked as failed.");
        }

        payment.Status = PaymentStatus.Failed;
        dbContext.PaymentStatusHistories.Add(new PaymentStatusHistory
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Status = PaymentStatus.Failed,
            ChangedAt = DateTimeOffset.UtcNow,
            Source = $"{AdminSource}:{actingAdminUserId}",
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = (await GetAdminRowsAsync(payment.Id, cancellationToken)).Single();
        return AdminPaymentActionOutcome.Success(updated);
    }

    private async Task<IReadOnlyList<AdminPaymentDto>> GetAdminRowsAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Select(payment => new AdminPaymentRow(
                payment.Id,
                payment.UserId,
                dbContext.Users.Where(user => user.Id == payment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                dbContext.Users.Where(user => user.Id == payment.UserId)
                    .Select(user => user.Email!).Single(),
                payment.ApplicationId,
                payment.Application != null && payment.Application.CourseOffering != null
                    ? payment.Application.CourseOffering.Course!.Title
                    : null,
                payment.Application != null && payment.Application.ProgramOffering != null
                    ? payment.Application.ProgramOffering.Program!.Title
                    : null,
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.CreatedAt,
                payment.CompletedAt))
            .ToListAsync(cancellationToken);

        return await AddStatusHistoryAsync(payments, cancellationToken);
    }

    private async Task<IReadOnlyList<AdminPaymentDto>> AddStatusHistoryAsync(
        IReadOnlyList<AdminPaymentRow> payments,
        CancellationToken cancellationToken)
    {
        var paymentIds = payments.Select(payment => payment.Id).ToArray();
        var history = await dbContext.PaymentStatusHistories
            .AsNoTracking()
            .Where(entry => paymentIds.Contains(entry.PaymentId))
            .OrderBy(entry => entry.ChangedAt)
            .Select(entry => new
            {
                entry.PaymentId,
                Dto = new PaymentStatusHistoryDto(entry.Id, entry.Status, entry.ChangedAt, entry.Source),
            })
            .ToListAsync(cancellationToken);

        var historiesByPayment = history
            .GroupBy(entry => entry.PaymentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PaymentStatusHistoryDto>)group.Select(entry => entry.Dto).ToArray());

        return payments.Select(payment => new AdminPaymentDto(
            payment.Id,
            payment.UserId,
            payment.LearnerName,
            payment.Email,
            payment.ApplicationId,
            payment.CourseTitle,
            payment.ProgramTitle,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.CreatedAt,
            payment.CompletedAt,
            historiesByPayment.GetValueOrDefault(payment.Id) ?? [])).ToArray();
    }

    private sealed record AdminPaymentRow(
        Guid Id,
        Guid UserId,
        string LearnerName,
        string Email,
        Guid ApplicationId,
        string? CourseTitle,
        string? ProgramTitle,
        decimal Amount,
        string Currency,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt);
}
