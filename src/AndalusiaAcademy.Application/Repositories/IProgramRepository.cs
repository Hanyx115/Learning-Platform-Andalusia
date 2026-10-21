using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface IProgramRepository
{
    Task<PaginatedResult<ProgramSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken);

    Task<ProgramDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken);

    void Add(AcademyProgram program);

    Task<AcademyProgram?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<ProgramSummaryDto?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
