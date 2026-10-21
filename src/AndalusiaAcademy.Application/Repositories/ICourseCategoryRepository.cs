using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICourseCategoryRepository
{
    Task<IReadOnlyList<CourseCategoryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken);

    void Add(CourseCategory category);

    Task<CourseCategory?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
