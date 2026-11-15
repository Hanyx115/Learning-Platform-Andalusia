using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICareerPathRepository
{
    Task<PaginatedResult<CareerPathSummaryDto>> GetPageAsync(
        CatalogListQuery query,
        bool includeNonPublic,
        CancellationToken cancellationToken);

    Task<CareerPathDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<CareerPathDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CareerPath?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken);

    Task<bool> CourseIdsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<bool> ProgramIdsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(
        CareerPath careerPath,
        IReadOnlyCollection<Guid> recommendedCourseIds,
        IReadOnlyCollection<Guid> recommendedProgramIds);

    Task ReplaceRecommendationsAsync(
        Guid careerPathId,
        IReadOnlyCollection<Guid> recommendedCourseIds,
        IReadOnlyCollection<Guid> recommendedProgramIds,
        CancellationToken cancellationToken);

    void Remove(CareerPath careerPath);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
