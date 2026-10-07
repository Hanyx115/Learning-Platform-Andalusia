using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class ProgramService(IProgramRepository programRepository)
{
    public Task<PaginatedResult<ProgramSummaryDto>> GetProgramsAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
        => programRepository.GetPageAsync(query, cancellationToken);

    public Task<ProgramDetailDto?> GetProgramBySlugAsync(string slug, CancellationToken cancellationToken)
        => programRepository.GetBySlugAsync(slug, cancellationToken);
}
