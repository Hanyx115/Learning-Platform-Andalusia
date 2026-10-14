using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class ProgramRepository(ApplicationDbContext dbContext) : IProgramRepository
{
    public async Task<PaginatedResult<ProgramSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
    {
        var programsQuery = dbContext.Programs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            programsQuery = programsQuery.Where(program =>
                program.Title.Contains(search) ||
                program.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            programsQuery = programsQuery.Where(program => program.Status == status);
        }

        var totalCount = await programsQuery.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.PageSize;

        if (offset > int.MaxValue)
        {
            return new PaginatedResult<ProgramSummaryDto>([], query.Page, query.PageSize, totalCount);
        }

        var orderedPrograms = query.Sort.ToLowerInvariant() switch
        {
            CatalogListQuery.TitleDescendingSort => programsQuery
                .OrderByDescending(program => program.Title)
                .ThenBy(program => program.Id),
            _ => programsQuery
                .OrderBy(program => program.Title)
                .ThenBy(program => program.Id),
        };

        var programs = await orderedPrograms
            .Select(program => new ProgramSummaryDto
            {
                Id = program.Id,
                Title = program.Title,
                Slug = program.Slug,
                Description = program.Description,
                Status = program.Status,
                Price = program.Price,
                CreatedAt = program.CreatedAt,
            })
            .Skip((int)offset)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProgramSummaryDto>(
            programs,
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<ProgramDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var program = await dbContext.Programs
            .AsNoTracking()
            .Where(program => program.Slug == slug)
            .Select(program => new ProgramDetailDto
            {
                Id = program.Id,
                Title = program.Title,
                Slug = program.Slug,
                Description = program.Description,
                Status = program.Status,
                Price = program.Price,
                CreatedAt = program.CreatedAt,
                Courses = dbContext.ProgramCourses
                    .Where(link => link.ProgramId == program.Id)
                    .OrderBy(link => link.SequenceOrder)
                    .Select(link => new ProgramCourseItemDto
                    {
                        CourseId = link.CourseId,
                        CourseTitle = link.Course!.Title,
                        CourseSlug = link.Course!.Slug,
                        SequenceOrder = link.SequenceOrder,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return program;
    }
}
