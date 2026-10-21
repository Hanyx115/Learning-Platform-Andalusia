using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICourseRepository
{
    Task<PaginatedResult<CourseDto>> GetPageAsync(
        CourseCatalogQuery query,
        CancellationToken cancellationToken);

    Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken);

    void Add(Course course);

    Task<Course?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
