using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseService(ICourseRepository courseRepository)
{
    public Task<IReadOnlyList<CourseDto>> GetCoursesAsync(CancellationToken cancellationToken)
        => courseRepository.GetAllAsync(cancellationToken);

    public Task<CourseDto?> GetCourseBySlugAsync(string slug, CancellationToken cancellationToken)
        => courseRepository.GetBySlugAsync(slug, cancellationToken);
}
