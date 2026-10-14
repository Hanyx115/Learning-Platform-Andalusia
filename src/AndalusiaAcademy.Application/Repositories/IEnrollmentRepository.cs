using AndalusiaAcademy.Application.DTOs.Enrollments;

namespace AndalusiaAcademy.Application.Repositories;

public interface IEnrollmentRepository
{
    Task<IReadOnlyList<EnrollmentDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
}
