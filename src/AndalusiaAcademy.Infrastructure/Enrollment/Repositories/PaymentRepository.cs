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
                    ? a.CourseOffering.Location!.Id
                    : a.ProgramOffering!.Location!.Id,
                LocationBranchName = a.CourseOffering != null
                    ? a.CourseOffering.Location!.BranchName
                    : a.ProgramOffering!.Location!.BranchName,
                LocationRoomName = a.CourseOffering != null
                    ? a.CourseOffering.Location!.RoomName
                    : a.ProgramOffering!.Location!.RoomName,
                LocationCity = a.CourseOffering != null
                    ? a.CourseOffering.Location!.City
                    : a.ProgramOffering!.Location!.City,
                LocationAddress = a.CourseOffering != null
                    ? a.CourseOffering.Location!.Address
                    : a.ProgramOffering!.Location!.Address,
                LocationMapUrl = a.CourseOffering != null
                    ? a.CourseOffering.Location!.MapUrl
                    : a.ProgramOffering!.Location!.MapUrl,
                LocationIsActive = a.CourseOffering != null
                    ? a.CourseOffering.Location!.IsActive
                    : a.ProgramOffering!.Location!.IsActive,
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

        var hasActivePayment = await dbContext.Payments
            .AsNoTracking()
            .AnyAsync(payment => payment.ApplicationId == applicationId && payment.Status != PaymentStatus.Failed, cancellationToken);

        if (hasActivePayment)
        {
            return PayForApplicationOutcome.Failure("A payment already exists for this application.");
        }

        var amount = application.CourseOfferingId.HasValue ? application.CoursePrice : application.ProgramPrice;

        if (amount is null)
        {
            return PayForApplicationOutcome.Failure("This course or program does not have a price set yet.");
        }

        var now = DateTimeOffset.UtcNow;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            UserId = userId,
            Amount = amount.Value,
            Currency = application.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = Guid.NewGuid(),
            CreatedAt = now,
        };

        dbContext.Payments.Add(payment);
        dbContext.PaymentStatusHistories.Add(new PaymentStatusHistory
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Status = PaymentStatus.Pending,
            ChangedAt = now,
            Source = Source,
        });

        // Persisted before calling the processor so a crash mid-call leaves a recoverable
        // Pending row instead of losing the payment attempt entirely (matches how the real,
        // asynchronous Paymob flow will behave once #7 replaces the simulated processor).
        await dbContext.SaveChangesAsync(cancellationToken);

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

        // Payment success and Enrollment creation commit in this one SaveChangesAsync call, so the
        // two can never be left out of sync with each other.
        await dbContext.SaveChangesAsync(cancellationToken);

        var paymentDto = new PaymentDto(
            payment.Id, payment.ApplicationId, payment.Amount, payment.Currency, payment.Status,
            payment.CreatedAt, payment.CompletedAt);

        var enrollmentDto = new EnrollmentDto(
            enrollment.Id, enrollment.ApplicationId, enrollment.PaymentId,
            enrollment.CourseOfferingId,
            application.CourseId, application.CourseTitle,
            enrollment.ProgramOfferingId,
            application.ProgramId, application.ProgramTitle,
            application.StartsAtUtc, application.EndsAtUtc,
            new TrainingLocationDto(
                application.LocationId,
                application.LocationBranchName,
                application.LocationRoomName,
                application.LocationCity,
                application.LocationAddress,
                application.LocationMapUrl,
                application.LocationIsActive),
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
                payment.CreatedAt, payment.CompletedAt))
            .ToListAsync(cancellationToken);
    }
}
