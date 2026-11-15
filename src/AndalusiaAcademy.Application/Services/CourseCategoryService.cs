using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseCategoryService(ICourseCategoryRepository repository)
{
    public Task<IReadOnlyList<CourseCategoryDto>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public async Task<SchedulingMutationOutcome<CourseCategoryDto>> CreateAsync(
        CreateCourseCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var slug = SlugGenerator.Generate(name);

        if (await repository.SlugExistsAsync(slug, excludedId: null, cancellationToken))
        {
            return Fail("A category with a matching name already exists.");
        }

        var category = new CourseCategory { Id = Guid.NewGuid(), Name = name, Slug = slug };
        repository.Add(category);
        await repository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<CourseCategoryDto>.Success(
            new CourseCategoryDto(category.Id, category.Name, category.Slug));
    }

    public async Task<SchedulingMutationOutcome<CourseCategoryDto>> UpdateAsync(
        Guid id, UpdateCourseCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await repository.GetForUpdateAsync(id, cancellationToken);
        if (category is null)
        {
            return SchedulingMutationOutcome<CourseCategoryDto>.Failure(
                SchedulingFailureKind.NotFound, "Category not found.");
        }

        var name = request.Name.Trim();
        var slug = SlugGenerator.Generate(name);

        if (await repository.SlugExistsAsync(slug, excludedId: id, cancellationToken))
        {
            return Fail("A category with a matching name already exists.");
        }

        category.Name = name;
        category.Slug = slug;
        await repository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<CourseCategoryDto>.Success(
            new CourseCategoryDto(category.Id, category.Name, category.Slug));
    }

    private static SchedulingMutationOutcome<CourseCategoryDto> Fail(string error)
        => SchedulingMutationOutcome<CourseCategoryDto>.Failure(SchedulingFailureKind.Conflict, error);
}
