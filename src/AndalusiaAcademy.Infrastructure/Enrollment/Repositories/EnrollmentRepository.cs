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
                enrollment.CourseId,
                enrollment.Course != null ? enrollment.Course.Title : null,
                enrollment.ProgramId,
                enrollment.Program != null ? enrollment.Program.Title : null,
                enrollment.Status,
                enrollment.EnrolledAt,
                enrollment.CompletedAt,
                enrollment.CancelledAt))
            .ToListAsync(cancellationToken);
    }
}
