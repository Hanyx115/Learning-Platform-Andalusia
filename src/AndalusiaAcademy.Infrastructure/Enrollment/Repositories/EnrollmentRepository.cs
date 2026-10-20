using AndalusiaAcademy.Application.DTOs.Enrollments;
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
                enrollment.Status,
                enrollment.EnrolledAt,
                enrollment.CompletedAt,
                enrollment.CancelledAt))
            .ToListAsync(cancellationToken);
    }
}
