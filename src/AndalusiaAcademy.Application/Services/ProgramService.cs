using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class ProgramService(IProgramRepository programRepository, TimeProvider timeProvider)
{
    public Task<PaginatedResult<ProgramSummaryDto>> GetProgramsAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
        => programRepository.GetPageAsync(query, cancellationToken);

    public Task<ProgramDetailDto?> GetProgramBySlugAsync(string slug, CancellationToken cancellationToken)
        => programRepository.GetBySlugAsync(slug, cancellationToken);

    public async Task<SchedulingMutationOutcome<ProgramSummaryDto>> CreateAsync(
        CreateProgramRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);

        if (await programRepository.SlugExistsAsync(slug, excludedId: null, cancellationToken))
        {
            return Fail("A program with a matching title already exists.");
        }

        var program = new AcademyProgram
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Description = request.Description.Trim(),
            Status = request.Status,
            Price = request.Price,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        programRepository.Add(program);
        await programRepository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<ProgramSummaryDto>.Success(
            (await programRepository.GetSummaryByIdAsync(program.Id, cancellationToken))!);
    }

    public async Task<SchedulingMutationOutcome<ProgramSummaryDto>> UpdateAsync(
        Guid id, UpdateProgramRequest request, CancellationToken cancellationToken)
    {
        var program = await programRepository.GetForUpdateAsync(id, cancellationToken);
        if (program is null)
        {
            return SchedulingMutationOutcome<ProgramSummaryDto>.Failure(
                SchedulingFailureKind.NotFound, "Program not found.");
        }

        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);

        if (await programRepository.SlugExistsAsync(slug, excludedId: id, cancellationToken))
        {
            return Fail("A program with a matching title already exists.");
        }

        program.Title = title;
        program.Slug = slug;
        program.Description = request.Description.Trim();
        program.Status = request.Status;
        program.Price = request.Price;

        await programRepository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<ProgramSummaryDto>.Success(
            (await programRepository.GetSummaryByIdAsync(id, cancellationToken))!);
    }

    private static SchedulingMutationOutcome<ProgramSummaryDto> Fail(string error)
        => SchedulingMutationOutcome<ProgramSummaryDto>.Failure(SchedulingFailureKind.Conflict, error);
}
