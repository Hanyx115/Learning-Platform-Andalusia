using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class ProgramService(IProgramRepository programRepository)
{
    public Task<IReadOnlyList<ProgramSummaryDto>> GetProgramsAsync(CancellationToken cancellationToken)
        => programRepository.GetAllAsync(cancellationToken);

    public Task<ProgramDetailDto?> GetProgramBySlugAsync(string slug, CancellationToken cancellationToken)
        => programRepository.GetBySlugAsync(slug, cancellationToken);
}
