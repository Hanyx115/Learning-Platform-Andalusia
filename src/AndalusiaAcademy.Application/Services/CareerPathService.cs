using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CareerPathService(ICareerPathRepository careerPathRepository)
{
    public Task<PaginatedResult<CareerPathSummaryDto>> GetCareerPathsAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
        => careerPathRepository.GetPageAsync(query, cancellationToken);

    public Task<CareerPathDetailDto?> GetCareerPathBySlugAsync(string slug, CancellationToken cancellationToken)
        => careerPathRepository.GetBySlugAsync(slug, cancellationToken);
}
