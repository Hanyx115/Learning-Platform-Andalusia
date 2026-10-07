using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class CareerPathRepository(ApplicationDbContext dbContext) : ICareerPathRepository
{
    public async Task<PaginatedResult<CareerPathSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
    {
        var careerPathsQuery = dbContext.CareerPaths.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            careerPathsQuery = careerPathsQuery.Where(careerPath =>
                careerPath.Title.Contains(search) ||
                careerPath.Description.Contains(search) ||
                careerPath.RecommendedSkills.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            careerPathsQuery = careerPathsQuery.Where(careerPath => careerPath.Status == status);
        }

        var totalCount = await careerPathsQuery.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.PageSize;

        if (offset > int.MaxValue)
        {
            return new PaginatedResult<CareerPathSummaryDto>([], query.Page, query.PageSize, totalCount);
        }

        var orderedCareerPaths = query.Sort.ToLowerInvariant() switch
        {
            CatalogListQuery.TitleDescendingSort => careerPathsQuery
                .OrderByDescending(careerPath => careerPath.Title)
                .ThenBy(careerPath => careerPath.Id),
            _ => careerPathsQuery
                .OrderBy(careerPath => careerPath.Title)
                .ThenBy(careerPath => careerPath.Id),
        };

        var careerPaths = await orderedCareerPaths
            .Select(careerPath => new CareerPathSummaryDto
            {
                Id = careerPath.Id,
                Title = careerPath.Title,
                Slug = careerPath.Slug,
                Description = careerPath.Description,
                Status = careerPath.Status,
            })
            .Skip((int)offset)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CareerPathSummaryDto>(
            careerPaths,
            query.Page,
            query.PageSize,
            totalCount);
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
