using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICourseRepository
{
    Task<PaginatedResult<CourseDto>> GetPageAsync(
        CourseCatalogQuery query,
        CancellationToken cancellationToken);

    Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
