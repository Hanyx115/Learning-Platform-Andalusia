using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICareerPathRepository
{
    Task<IReadOnlyList<CareerPathSummaryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<CareerPathDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
