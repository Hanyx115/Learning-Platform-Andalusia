using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class CareerPathRepository(ApplicationDbContext dbContext) : ICareerPathRepository
{
    public async Task<PaginatedResult<CareerPathSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        bool includeNonPublic,
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

        careerPathsQuery = includeNonPublic && !string.IsNullOrWhiteSpace(query.Status)
            ? careerPathsQuery.Where(careerPath => careerPath.Status == query.Status.Trim())
            : careerPathsQuery.Where(careerPath => careerPath.Status == CatalogStatuses.Published);

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
                ImageUrl = careerPath.ImageUrl,
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
            .Where(careerPath => careerPath.Slug == slug && careerPath.Status == CatalogStatuses.Published)
            .Select(careerPath => new CareerPathDetailDto
            {
                Id = careerPath.Id,
                Title = careerPath.Title,
                Slug = careerPath.Slug,
                Description = careerPath.Description,
                RecommendedSkills = careerPath.RecommendedSkills,
                Status = careerPath.Status,
                ImageUrl = careerPath.ImageUrl,
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

    public Task<CareerPathDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => ProjectDetails(dbContext.CareerPaths.AsNoTracking().Where(careerPath => careerPath.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CareerPath?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CareerPaths.FirstOrDefaultAsync(careerPath => careerPath.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.CareerPaths.AnyAsync(
            careerPath => careerPath.Slug == slug && (excludedId == null || careerPath.Id != excludedId),
            cancellationToken);

    public async Task<bool> CourseIdsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => ids.Count == 0 || await dbContext.Courses.CountAsync(course => ids.Contains(course.Id), cancellationToken)
            == ids.Distinct().Count();

    public async Task<bool> ProgramIdsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => ids.Count == 0 || await dbContext.Programs.CountAsync(program => ids.Contains(program.Id), cancellationToken)
            == ids.Distinct().Count();

    public void Add(
        CareerPath careerPath,
        IReadOnlyCollection<Guid> recommendedCourseIds,
        IReadOnlyCollection<Guid> recommendedProgramIds)
    {
        dbContext.CareerPaths.Add(careerPath);
        dbContext.CareerPathCourses.AddRange(recommendedCourseIds.Select(courseId => new CareerPathCourse
        {
            CareerPathId = careerPath.Id,
            CourseId = courseId,
        }));
        dbContext.CareerPathPrograms.AddRange(recommendedProgramIds.Select(programId => new CareerPathProgram
        {
            CareerPathId = careerPath.Id,
            ProgramId = programId,
        }));
    }

    public async Task ReplaceRecommendationsAsync(
        Guid careerPathId,
        IReadOnlyCollection<Guid> recommendedCourseIds,
        IReadOnlyCollection<Guid> recommendedProgramIds,
        CancellationToken cancellationToken)
    {
        var courseLinks = await dbContext.CareerPathCourses
            .Where(link => link.CareerPathId == careerPathId)
            .ToListAsync(cancellationToken);
        var programLinks = await dbContext.CareerPathPrograms
            .Where(link => link.CareerPathId == careerPathId)
            .ToListAsync(cancellationToken);

        dbContext.CareerPathCourses.RemoveRange(courseLinks);
        dbContext.CareerPathPrograms.RemoveRange(programLinks);
        dbContext.CareerPathCourses.AddRange(recommendedCourseIds.Select(courseId => new CareerPathCourse
        {
            CareerPathId = careerPathId,
            CourseId = courseId,
        }));
        dbContext.CareerPathPrograms.AddRange(recommendedProgramIds.Select(programId => new CareerPathProgram
        {
            CareerPathId = careerPathId,
            ProgramId = programId,
        }));
    }

    public void Remove(CareerPath careerPath) => dbContext.CareerPaths.Remove(careerPath);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<CareerPathDetailDto> ProjectDetails(IQueryable<CareerPath> query)
        => query.Select(careerPath => new CareerPathDetailDto
        {
            Id = careerPath.Id,
            Title = careerPath.Title,
            Slug = careerPath.Slug,
            Description = careerPath.Description,
            RecommendedSkills = careerPath.RecommendedSkills,
            Status = careerPath.Status,
            ImageUrl = careerPath.ImageUrl,
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
        });
}
