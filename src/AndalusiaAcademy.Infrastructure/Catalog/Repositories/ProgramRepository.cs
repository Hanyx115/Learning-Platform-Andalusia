using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class ProgramRepository(ApplicationDbContext dbContext) : IProgramRepository
{
    public async Task<IReadOnlyList<ProgramSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Programs
            .AsNoTracking()
            .OrderBy(program => program.Title)
            .Select(program => new ProgramSummaryDto
            {
                Id = program.Id,
                Title = program.Title,
                Slug = program.Slug,
                Description = program.Description,
                Status = program.Status,
                CreatedAt = program.CreatedAt,
            })
            .ToListAsync(cancellationToken);
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
