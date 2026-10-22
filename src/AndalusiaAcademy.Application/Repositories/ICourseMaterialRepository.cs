using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICourseMaterialRepository
{
    Task<bool> CourseExistsAsync(Guid courseId, CancellationToken cancellationToken);

    /// <summary>True if the user is assigned as instructor to at least one session within any offering of the course.</summary>
    Task<bool> IsInstructorForCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);

    Task<bool> SessionBelongsToCourseAsync(
        Guid courseSessionId,
        Guid courseId,
        CancellationToken cancellationToken);

    Task<CourseMaterial?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<CourseMaterialDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseMaterialDto>> GetForCourseManagementAsync(
        Guid courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseMaterialDto>?> GetForLearnerEnrollmentAsync(
        Guid enrollmentId,
        Guid userId,
        CancellationToken cancellationToken);

    void Add(CourseMaterial material);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
