using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CareerPathService(ICareerPathRepository careerPathRepository)
{
    public Task<IReadOnlyList<CareerPathSummaryDto>> GetCareerPathsAsync(CancellationToken cancellationToken)
        => careerPathRepository.GetAllAsync(cancellationToken);

    public Task<CareerPathDetailDto?> GetCareerPathBySlugAsync(string slug, CancellationToken cancellationToken)
        => careerPathRepository.GetBySlugAsync(slug, cancellationToken);
}
