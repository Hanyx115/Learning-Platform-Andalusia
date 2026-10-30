using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class ProgramRepository(ApplicationDbContext dbContext) : IProgramRepository
{
    public async Task<PaginatedResult<ProgramSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        bool includeNonPublic,
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

        // Public callers are always restricted to Published. Authenticated management endpoints
        // opt in to non-public statuses through includeNonPublic.
        programsQuery = includeNonPublic && !string.IsNullOrWhiteSpace(query.Status)
            ? programsQuery.Where(program => program.Status == query.Status.Trim())
            : programsQuery.Where(program => program.Status == CatalogStatuses.Published);

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
            .Where(program => program.Slug == slug && program.Status == CatalogStatuses.Published)
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

    public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.Programs.AnyAsync(
            program => program.Slug == slug && (excludedId == null || program.Id != excludedId),
            cancellationToken);

    public void Add(AcademyProgram program) => dbContext.Programs.Add(program);

    public Task<AcademyProgram?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Programs.FirstOrDefaultAsync(program => program.Id == id, cancellationToken);

    public Task<ProgramSummaryDto?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Programs
            .AsNoTracking()
            .Where(program => program.Id == id)
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
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
