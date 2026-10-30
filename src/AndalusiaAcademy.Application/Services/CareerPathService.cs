using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CareerPathService(ICareerPathRepository careerPathRepository)
{
    public Task<PaginatedResult<CareerPathSummaryDto>> GetCareerPathsAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
        => careerPathRepository.GetPageAsync(query, includeNonPublic: false, cancellationToken);

    public Task<PaginatedResult<CareerPathSummaryDto>> GetCareerPathsForManagementAsync(
        CatalogListQuery query,
        CancellationToken cancellationToken)
        => careerPathRepository.GetPageAsync(query, includeNonPublic: true, cancellationToken);

    public Task<CareerPathDetailDto?> GetCareerPathBySlugAsync(string slug, CancellationToken cancellationToken)
        => careerPathRepository.GetBySlugAsync(slug, cancellationToken);

    public Task<CareerPathDetailDto?> GetCareerPathByIdAsync(Guid id, CancellationToken cancellationToken)
        => careerPathRepository.GetByIdAsync(id, cancellationToken);

    public async Task<SchedulingMutationOutcome<CareerPathDetailDto>> CreateAsync(
        CareerPathRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = await ValidateReferencesAsync(request, cancellationToken);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);
        if (await careerPathRepository.SlugExistsAsync(slug, excludedId: null, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Conflict, "A career path with a matching title already exists.");
        }

        var careerPath = new CareerPath
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Description = request.Description.Trim(),
            RecommendedSkills = request.RecommendedSkills.Trim(),
            Status = request.Status,
        };

        careerPathRepository.Add(
            careerPath,
            DistinctIds(request.RecommendedCourseIds),
            DistinctIds(request.RecommendedProgramIds));
        await careerPathRepository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<CareerPathDetailDto>.Success(
            (await careerPathRepository.GetByIdAsync(careerPath.Id, cancellationToken))!);
    }

    public async Task<SchedulingMutationOutcome<CareerPathDetailDto>> UpdateAsync(
        Guid id,
        CareerPathRequest request,
        CancellationToken cancellationToken)
    {
        var careerPath = await careerPathRepository.GetForUpdateAsync(id, cancellationToken);
        if (careerPath is null)
        {
            return Fail(SchedulingFailureKind.NotFound, "Career path not found.");
        }

        var validationFailure = await ValidateReferencesAsync(request, cancellationToken);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);
        if (await careerPathRepository.SlugExistsAsync(slug, excludedId: id, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Conflict, "A career path with a matching title already exists.");
        }

        careerPath.Title = title;
        careerPath.Slug = slug;
        careerPath.Description = request.Description.Trim();
        careerPath.RecommendedSkills = request.RecommendedSkills.Trim();
        careerPath.Status = request.Status;

        await careerPathRepository.ReplaceRecommendationsAsync(
            id,
            DistinctIds(request.RecommendedCourseIds),
            DistinctIds(request.RecommendedProgramIds),
            cancellationToken);
        await careerPathRepository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<CareerPathDetailDto>.Success(
            (await careerPathRepository.GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var careerPath = await careerPathRepository.GetForUpdateAsync(id, cancellationToken);
        if (careerPath is null)
        {
            return false;
        }

        careerPathRepository.Remove(careerPath);
        await careerPathRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<SchedulingMutationOutcome<CareerPathDetailDto>?> ValidateReferencesAsync(
        CareerPathRequest request,
        CancellationToken cancellationToken)
    {
        var courseIds = DistinctIds(request.RecommendedCourseIds);
        if (!await careerPathRepository.CourseIdsExistAsync(courseIds, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Validation, "Select valid recommended courses.");
        }

        var programIds = DistinctIds(request.RecommendedProgramIds);
        return await careerPathRepository.ProgramIdsExistAsync(programIds, cancellationToken)
            ? null
            : Fail(SchedulingFailureKind.Validation, "Select valid recommended programs.");
    }

    private static Guid[] DistinctIds(IReadOnlyCollection<Guid>? ids)
        => ids?.Distinct().ToArray() ?? [];

    private static SchedulingMutationOutcome<CareerPathDetailDto> Fail(
        SchedulingFailureKind kind,
        string error)
        => SchedulingMutationOutcome<CareerPathDetailDto>.Failure(kind, error);
}
