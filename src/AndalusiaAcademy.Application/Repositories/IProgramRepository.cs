using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface IProgramRepository
{
    Task<IReadOnlyList<ProgramSummaryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProgramDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
