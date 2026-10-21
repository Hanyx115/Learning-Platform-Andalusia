using AndalusiaAcademy.Application.DTOs.Enrollments;
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
                enrollment.CourseOffering != null
                    ? new TrainingLocationDto(
                        enrollment.CourseOffering.Location!.Id,
                        enrollment.CourseOffering.Location.BranchName,
                        enrollment.CourseOffering.Location.RoomName,
                        enrollment.CourseOffering.Location.City,
                        enrollment.CourseOffering.Location.Address,
                        enrollment.CourseOffering.Location.MapUrl,
                        enrollment.CourseOffering.Location.IsActive)
                    : new TrainingLocationDto(
                        enrollment.ProgramOffering!.Location!.Id,
                        enrollment.ProgramOffering.Location.BranchName,
                        enrollment.ProgramOffering.Location.RoomName,
                        enrollment.ProgramOffering.Location.City,
                        enrollment.ProgramOffering.Location.Address,
                        enrollment.ProgramOffering.Location.MapUrl,
                        enrollment.ProgramOffering.Location.IsActive),
                enrollment.Status,
                enrollment.EnrolledAt,
                enrollment.CompletedAt,
                enrollment.CancelledAt))
            .ToListAsync(cancellationToken);
    }
}
