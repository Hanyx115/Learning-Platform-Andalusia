using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICareerPathRepository
{
    Task<PaginatedResult<CareerPathSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken);

    Task<CareerPathDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
