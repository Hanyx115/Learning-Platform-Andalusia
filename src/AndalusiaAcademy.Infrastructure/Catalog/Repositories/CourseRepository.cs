using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class CourseRepository(ApplicationDbContext dbContext) : ICourseRepository
{
    public async Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .OrderBy(course => course.Title)
            .Select(course => new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                Status = course.Status,
                Type = course.Type,
                CategoryName = course.Category!.Name,
                CreatedAt = course.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Slug == slug)
            .Select(course => new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                Status = course.Status,
                Type = course.Type,
                CategoryName = course.Category!.Name,
                CreatedAt = course.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
