using AndalusiaAcademy.Application.DTOs.Operations;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Operations.Repositories;

public sealed class OperationsDashboardRepository(ApplicationDbContext dbContext)
    : IOperationsDashboardRepository
{
    public async Task<OperationsOverviewDto> GetOverviewAsync(
        DateTimeOffset now,
        int recentActivityLimit,
        CancellationToken cancellationToken)
    {
        var activeStudents = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.Status == EnrollmentStatus.Active)
            .Join(
                dbContext.Users.AsNoTracking().Where(user => user.IsActive),
                enrollment => enrollment.UserId,
                user => user.Id,
                (enrollment, user) => user.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        var activeInstructors = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.IsActive && role.Name == RoleNames.Instructor
                select user.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        var runningCourses = await dbContext.CourseOfferings
            .AsNoTracking()
            .CountAsync(
                offering => offering.Status != OfferingStatuses.Cancelled
                    && offering.StartsAtUtc <= now
                    && offering.EndsAtUtc >= now,
                cancellationToken);

        var pendingApplications = await dbContext.LearnerApplications
            .AsNoTracking()
            .CountAsync(application => application.Status == ApplicationStatus.Pending, cancellationToken);

        var activeEnrollments = await dbContext.Enrollments
            .AsNoTracking()
            .CountAsync(enrollment => enrollment.Status == EnrollmentStatus.Active, cancellationToken);

        var revenueRows = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Succeeded)
            .GroupBy(payment => payment.Currency)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(payment => payment.Amount),
                PaymentCount = group.Count(),
            })
            .OrderBy(summary => summary.Currency)
            .ToListAsync(cancellationToken);
        var revenue = revenueRows
            .Select(row => new RevenueSummaryDto(row.Currency, row.Amount, row.PaymentCount))
            .ToList();

        var applicationStatusRows = await dbContext.LearnerApplications
            .AsNoTracking()
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(summary => summary.Status)
            .ToListAsync(cancellationToken);
        var applicationsByStatus = applicationStatusRows
            .Select(row => new StatusSummaryDto(row.Status, row.Count))
            .ToList();

        var paymentStatusRows = await dbContext.Payments
            .AsNoTracking()
            .GroupBy(payment => payment.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(summary => summary.Status)
            .ToListAsync(cancellationToken);
        var paymentsByStatus = paymentStatusRows
            .Select(row => new StatusSummaryDto(row.Status, row.Count))
            .ToList();

        var enrollmentStatusRows = await dbContext.Enrollments
            .AsNoTracking()
            .GroupBy(enrollment => enrollment.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(summary => summary.Status)
            .ToListAsync(cancellationToken);
        var enrollmentsByStatus = enrollmentStatusRows
            .Select(row => new StatusSummaryDto(row.Status, row.Count))
            .ToList();

        var recentActivity = await GetRecentActivityAsync(recentActivityLimit, cancellationToken);

        return new OperationsOverviewDto(
            activeStudents,
            activeInstructors,
            runningCourses,
            pendingApplications,
            activeEnrollments,
            revenue,
            applicationsByStatus,
            paymentsByStatus,
            enrollmentsByStatus,
            recentActivity);
    }

    private async Task<IReadOnlyList<OperationsActivityDto>> GetRecentActivityAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var applicationActivity = await dbContext.LearnerApplicationStatusHistories
            .AsNoTracking()
            .OrderByDescending(history => history.ChangedAt)
            .Take(limit)
            .Select(history => new OperationsActivityDto(
                history.Id,
                "Application",
                history.Status,
                history.ChangedAt,
                dbContext.Users
                    .Where(user => user.Id == history.Application!.UserId)
                    .Select(user => user.FirstName + " " + user.LastName)
                    .FirstOrDefault() ?? string.Empty,
                history.Application!.CourseOfferingId != null
                    ? history.Application.CourseOffering!.Course!.Title
                    : history.Application.ProgramOffering!.Program!.Title))
            .ToListAsync(cancellationToken);

        var paymentActivity = await dbContext.PaymentStatusHistories
            .AsNoTracking()
            .OrderByDescending(history => history.ChangedAt)
            .Take(limit)
            .Select(history => new OperationsActivityDto(
                history.Id,
                "Payment",
                history.Status,
                history.ChangedAt,
                dbContext.Users
                    .Where(user => user.Id == history.Payment!.UserId)
                    .Select(user => user.FirstName + " " + user.LastName)
                    .FirstOrDefault() ?? string.Empty,
                history.Payment!.Application!.CourseOfferingId != null
                    ? history.Payment.Application.CourseOffering!.Course!.Title
                    : history.Payment.Application.ProgramOffering!.Program!.Title))
            .ToListAsync(cancellationToken);

        var enrollmentActivity = await dbContext.Enrollments
            .AsNoTracking()
            .OrderByDescending(enrollment => enrollment.EnrolledAt)
            .Take(limit)
            .Select(enrollment => new OperationsActivityDto(
                enrollment.Id,
                "Enrollment",
                enrollment.Status,
                enrollment.EnrolledAt,
                dbContext.Users
                    .Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName)
                    .FirstOrDefault() ?? string.Empty,
                enrollment.CourseOfferingId != null
                    ? enrollment.CourseOffering!.Course!.Title
                    : enrollment.ProgramOffering!.Program!.Title))
            .ToListAsync(cancellationToken);

        return applicationActivity
            .Concat(paymentActivity)
            .Concat(enrollmentActivity)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .Take(limit)
            .ToList();
    }
}
