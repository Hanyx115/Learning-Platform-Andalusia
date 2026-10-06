using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class CareerPathRepository(ApplicationDbContext dbContext) : ICareerPathRepository
{
    public async Task<IReadOnlyList<CareerPathSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CareerPaths
            .AsNoTracking()
            .OrderBy(careerPath => careerPath.Title)
            .Select(careerPath => new CareerPathSummaryDto
            {
                Id = careerPath.Id,
                Title = careerPath.Title,
                Slug = careerPath.Slug,
                Description = careerPath.Description,
                Status = careerPath.Status,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CareerPathDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var careerPath = await dbContext.CareerPaths
            .AsNoTracking()
            .Where(careerPath => careerPath.Slug == slug)
            .Select(careerPath => new CareerPathDetailDto
            {
                Id = careerPath.Id,
                Title = careerPath.Title,
                Slug = careerPath.Slug,
                Description = careerPath.Description,
                RecommendedSkills = careerPath.RecommendedSkills,
                Status = careerPath.Status,
                RecommendedPrograms = dbContext.CareerPathPrograms
                    .Where(link => link.CareerPathId == careerPath.Id)
                    .Select(link => new CareerPathProgramItemDto
                    {
                        ProgramId = link.ProgramId,
                        ProgramTitle = link.Program!.Title,
                        ProgramSlug = link.Program!.Slug,
                    })
                    .ToList(),
                RecommendedCourses = dbContext.CareerPathCourses
                    .Where(link => link.CareerPathId == careerPath.Id)
                    .Select(link => new CareerPathCourseItemDto
                    {
                        CourseId = link.CourseId,
                        CourseTitle = link.Course!.Title,
                        CourseSlug = link.Course!.Slug,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return careerPath;
    }
}
