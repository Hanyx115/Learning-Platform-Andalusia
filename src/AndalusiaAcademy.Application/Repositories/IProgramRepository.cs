using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface IProgramRepository
{
    Task<PaginatedResult<ProgramSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken);

    Task<ProgramDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
