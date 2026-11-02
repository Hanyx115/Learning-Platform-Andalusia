using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Enrollments;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Repositories;

public sealed class EnrollmentRepository(ApplicationDbContext dbContext) : IEnrollmentRepository
{
    public async Task<IReadOnlyList<EnrollmentDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId)
            .OrderByDescending(enrollment => enrollment.EnrolledAt)
            .Select(enrollment => new EnrollmentDto(
                enrollment.Id,
                enrollment.ApplicationId,
                enrollment.PaymentId,
                enrollment.CourseOfferingId,
                enrollment.CourseOffering != null ? enrollment.CourseOffering.CourseId : null,
                enrollment.CourseOffering != null ? enrollment.CourseOffering.Course!.Title : null,
                enrollment.ProgramOfferingId,
                enrollment.ProgramOffering != null ? enrollment.ProgramOffering.ProgramId : null,
                enrollment.ProgramOffering != null ? enrollment.ProgramOffering.Program!.Title : null,
                enrollment.CourseOffering != null
                    ? enrollment.CourseOffering.StartsAtUtc
                    : enrollment.ProgramOffering!.StartsAtUtc,
                enrollment.CourseOffering != null
                    ? enrollment.CourseOffering.EndsAtUtc
                    : enrollment.ProgramOffering!.EndsAtUtc,
                enrollment.CourseOffering != null && enrollment.CourseOffering.Location != null
                    ? new TrainingLocationDto(
                        enrollment.CourseOffering.Location.Id,
                        enrollment.CourseOffering.Location.BranchName,
                        enrollment.CourseOffering.Location.RoomName,
                        enrollment.CourseOffering.Location.City,
                        enrollment.CourseOffering.Location.Address,
                        enrollment.CourseOffering.Location.MapUrl,
                        enrollment.CourseOffering.Location.Capacity,
                        enrollment.CourseOffering.Location.IsActive)
                    : enrollment.ProgramOffering != null && enrollment.ProgramOffering.Location != null
                    ? new TrainingLocationDto(
                        enrollment.ProgramOffering.Location.Id,
                        enrollment.ProgramOffering.Location.BranchName,
                        enrollment.ProgramOffering.Location.RoomName,
                        enrollment.ProgramOffering.Location.City,
                        enrollment.ProgramOffering.Location.Address,
                        enrollment.ProgramOffering.Location.MapUrl,
                        enrollment.ProgramOffering.Location.Capacity,
                        enrollment.ProgramOffering.Location.IsActive)
                    : null,
                enrollment.Status,
                enrollment.EnrolledAt,
                enrollment.CompletedAt,
                enrollment.CancelledAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminEnrollmentDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Enrollments
            .AsNoTracking()
            .OrderByDescending(enrollment => enrollment.EnrolledAt)
            .Select(enrollment => new AdminEnrollmentDto(
                enrollment.Id,
                enrollment.UserId,
                dbContext.Users.Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                dbContext.Users.Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.Email!).Single(),
                enrollment.ApplicationId,
                enrollment.PaymentId,
                enrollment.CourseOfferingId,
                enrollment.CourseOffering != null ? enrollment.CourseOffering.CourseId : null,
                enrollment.CourseOffering != null ? enrollment.CourseOffering.Course!.Title : null,
                enrollment.ProgramOfferingId,
                enrollment.ProgramOffering != null ? enrollment.ProgramOffering.ProgramId : null,
                enrollment.ProgramOffering != null ? enrollment.ProgramOffering.Program!.Title : null,
                enrollment.CourseOffering != null
                    ? enrollment.CourseOffering.StartsAtUtc
                    : enrollment.ProgramOffering!.StartsAtUtc,
                enrollment.CourseOffering != null
                    ? enrollment.CourseOffering.EndsAtUtc
                    : enrollment.ProgramOffering!.EndsAtUtc,
                enrollment.CourseOffering != null && enrollment.CourseOffering.Location != null
                    ? new TrainingLocationDto(
                        enrollment.CourseOffering.Location.Id,
                        enrollment.CourseOffering.Location.BranchName,
                        enrollment.CourseOffering.Location.RoomName,
                        enrollment.CourseOffering.Location.City,
                        enrollment.CourseOffering.Location.Address,
                        enrollment.CourseOffering.Location.MapUrl,
                        enrollment.CourseOffering.Location.Capacity,
                        enrollment.CourseOffering.Location.IsActive)
                    : enrollment.ProgramOffering != null && enrollment.ProgramOffering.Location != null
                    ? new TrainingLocationDto(
                        enrollment.ProgramOffering.Location.Id,
                        enrollment.ProgramOffering.Location.BranchName,
                        enrollment.ProgramOffering.Location.RoomName,
                        enrollment.ProgramOffering.Location.City,
                        enrollment.ProgramOffering.Location.Address,
                        enrollment.ProgramOffering.Location.MapUrl,
                        enrollment.ProgramOffering.Location.Capacity,
                        enrollment.ProgramOffering.Location.IsActive)
                    : null,
                enrollment.Status,
                enrollment.EnrolledAt,
                enrollment.CompletedAt,
                enrollment.CancelledAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginatedResult<AdminEnrollmentDto>> GetPageAsync(
        EnrollmentDirectoryQuery query,
        CancellationToken cancellationToken)
    {
        var enrollments =
            from enrollment in dbContext.Enrollments.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on enrollment.UserId equals user.Id
            select new { Enrollment = enrollment, User = user };

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            enrollments = enrollments.Where(entry =>
                entry.User.FirstName.Contains(search) ||
                entry.User.LastName.Contains(search) ||
                (entry.User.FirstName + " " + entry.User.LastName).Contains(search) ||
                (entry.User.Email != null && entry.User.Email.Contains(search)) ||
                (entry.Enrollment.CourseOffering != null &&
                    entry.Enrollment.CourseOffering.Course!.Title.Contains(search)) ||
                (entry.Enrollment.ProgramOffering != null &&
                    entry.Enrollment.ProgramOffering.Program!.Title.Contains(search)));
        }

        var status = query.Status?.Trim().ToLowerInvariant() switch
        {
            "active" => EnrollmentStatus.Active,
            "completed" => EnrollmentStatus.Completed,
            "cancelled" => EnrollmentStatus.Cancelled,
            _ => null,
        };
        if (status is not null)
        {
            enrollments = enrollments.Where(entry => entry.Enrollment.Status == status);
        }

        var type = query.Type?.Trim();
        if (string.Equals(type, "Course", StringComparison.OrdinalIgnoreCase))
        {
            enrollments = enrollments.Where(entry => entry.Enrollment.CourseOfferingId != null);
        }
        else if (string.Equals(type, "Program", StringComparison.OrdinalIgnoreCase))
        {
            enrollments = enrollments.Where(entry => entry.Enrollment.ProgramOfferingId != null);
        }

        var totalCount = await enrollments.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.PageSize;
        if (offset > int.MaxValue)
        {
            return new PaginatedResult<AdminEnrollmentDto>([], query.Page, query.PageSize, totalCount);
        }

        var items = await enrollments
            .OrderByDescending(entry => entry.Enrollment.EnrolledAt)
            .ThenBy(entry => entry.User.FirstName)
            .ThenBy(entry => entry.User.LastName)
            .Skip((int)offset)
            .Take(query.PageSize)
            .Select(entry => new AdminEnrollmentDto(
                entry.Enrollment.Id,
                entry.Enrollment.UserId,
                entry.User.FirstName + " " + entry.User.LastName,
                entry.User.Email!,
                entry.Enrollment.ApplicationId,
                entry.Enrollment.PaymentId,
                entry.Enrollment.CourseOfferingId,
                entry.Enrollment.CourseOffering != null ? entry.Enrollment.CourseOffering.CourseId : null,
                entry.Enrollment.CourseOffering != null ? entry.Enrollment.CourseOffering.Course!.Title : null,
                entry.Enrollment.ProgramOfferingId,
                entry.Enrollment.ProgramOffering != null ? entry.Enrollment.ProgramOffering.ProgramId : null,
                entry.Enrollment.ProgramOffering != null ? entry.Enrollment.ProgramOffering.Program!.Title : null,
                entry.Enrollment.CourseOffering != null
                    ? entry.Enrollment.CourseOffering.StartsAtUtc
                    : entry.Enrollment.ProgramOffering!.StartsAtUtc,
                entry.Enrollment.CourseOffering != null
                    ? entry.Enrollment.CourseOffering.EndsAtUtc
                    : entry.Enrollment.ProgramOffering!.EndsAtUtc,
                entry.Enrollment.CourseOffering != null && entry.Enrollment.CourseOffering.Location != null
                    ? new TrainingLocationDto(
                        entry.Enrollment.CourseOffering.Location.Id,
                        entry.Enrollment.CourseOffering.Location.BranchName,
                        entry.Enrollment.CourseOffering.Location.RoomName,
                        entry.Enrollment.CourseOffering.Location.City,
                        entry.Enrollment.CourseOffering.Location.Address,
                        entry.Enrollment.CourseOffering.Location.MapUrl,
                        entry.Enrollment.CourseOffering.Location.Capacity,
                        entry.Enrollment.CourseOffering.Location.IsActive)
                    : entry.Enrollment.ProgramOffering != null && entry.Enrollment.ProgramOffering.Location != null
                    ? new TrainingLocationDto(
                        entry.Enrollment.ProgramOffering.Location.Id,
                        entry.Enrollment.ProgramOffering.Location.BranchName,
                        entry.Enrollment.ProgramOffering.Location.RoomName,
                        entry.Enrollment.ProgramOffering.Location.City,
                        entry.Enrollment.ProgramOffering.Location.Address,
                        entry.Enrollment.ProgramOffering.Location.MapUrl,
                        entry.Enrollment.ProgramOffering.Location.Capacity,
                        entry.Enrollment.ProgramOffering.Location.IsActive)
                    : null,
                entry.Enrollment.Status,
                entry.Enrollment.EnrolledAt,
                entry.Enrollment.CompletedAt,
                entry.Enrollment.CancelledAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<AdminEnrollmentDto>(items, query.Page, query.PageSize, totalCount);
    }
}
