using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
