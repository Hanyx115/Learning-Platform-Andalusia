using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Enrollments;

namespace AndalusiaAcademy.Application.Repositories;

public interface IEnrollmentRepository
{
    Task<IReadOnlyList<EnrollmentDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminEnrollmentDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<PaginatedResult<AdminEnrollmentDto>> GetPageAsync(
        EnrollmentDirectoryQuery query,
        CancellationToken cancellationToken);
}
