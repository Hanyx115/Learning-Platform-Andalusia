using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseService(ICourseRepository courseRepository)
{
    public Task<PaginatedResult<CourseDto>> GetCoursesAsync(
        CourseCatalogQuery query,
        CancellationToken cancellationToken)
        => courseRepository.GetPageAsync(query, cancellationToken);

    public Task<CourseDto?> GetCourseBySlugAsync(string slug, CancellationToken cancellationToken)
        => courseRepository.GetBySlugAsync(slug, cancellationToken);
}
