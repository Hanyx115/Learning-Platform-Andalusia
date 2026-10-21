using AndalusiaAcademy.Application.DTOs.Enrollments;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class EnrollmentReadService(IEnrollmentRepository enrollmentRepository)
{
    public Task<IReadOnlyList<EnrollmentDto>> GetMyEnrollmentsAsync(Guid userId, CancellationToken cancellationToken)
        => enrollmentRepository.GetForUserAsync(userId, cancellationToken);

    public Task<IReadOnlyList<AdminEnrollmentDto>> GetAllAsync(CancellationToken cancellationToken)
        => enrollmentRepository.GetAllAsync(cancellationToken);
}
