using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseService(ICourseRepository courseRepository, TimeProvider timeProvider)
{
    public Task<PaginatedResult<CourseDto>> GetCoursesAsync(
        CourseCatalogQuery query,
        CancellationToken cancellationToken)
        => courseRepository.GetPageAsync(query, includeNonPublic: false, cancellationToken);

    public Task<PaginatedResult<CourseDto>> GetCoursesForManagementAsync(
        CourseCatalogQuery query,
        CancellationToken cancellationToken)
        => courseRepository.GetPageAsync(query, includeNonPublic: true, cancellationToken);

    public Task<CourseDto?> GetCourseBySlugAsync(string slug, CancellationToken cancellationToken)
        => courseRepository.GetBySlugAsync(slug, cancellationToken);

    public async Task<SchedulingMutationOutcome<CourseDto>> CreateAsync(
        CreateCourseRequest request, CancellationToken cancellationToken)
    {
        if (!await courseRepository.CategoryExistsAsync(request.CategoryId, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Validation, "Select a valid category.");
        }

        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);

        if (await courseRepository.SlugExistsAsync(slug, excludedId: null, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Conflict, "A course with a matching title already exists.");
        }

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Title = title,
            Slug = slug,
            Description = request.Description.Trim(),
            Status = request.Status,
            Type = request.Type,
            Price = request.Price,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        courseRepository.Add(course);
        await courseRepository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<CourseDto>.Success(
            (await courseRepository.GetByIdAsync(course.Id, cancellationToken))!);
    }

    public async Task<SchedulingMutationOutcome<CourseDto>> UpdateAsync(
        Guid id, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetForUpdateAsync(id, cancellationToken);
        if (course is null)
        {
            return Fail(SchedulingFailureKind.NotFound, "Course not found.");
        }

        if (!await courseRepository.CategoryExistsAsync(request.CategoryId, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Validation, "Select a valid category.");
        }

        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);

        if (await courseRepository.SlugExistsAsync(slug, excludedId: id, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Conflict, "A course with a matching title already exists.");
        }

        course.CategoryId = request.CategoryId;
        course.Title = title;
        course.Slug = slug;
        course.Description = request.Description.Trim();
        course.Status = request.Status;
        course.Type = request.Type;
        course.Price = request.Price;

        await courseRepository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<CourseDto>.Success(
            (await courseRepository.GetByIdAsync(id, cancellationToken))!);
    }

    public Task<SchedulingMutationOutcome<CourseDto>> CreateContentAsync(
        CourseContentRequest request, CancellationToken cancellationToken)
        => CreateAsync(
            new CreateCourseRequest(
                request.Title,
                request.CategoryId,
                request.Description,
                request.Status,
                request.Type,
                Price: null),
            cancellationToken);

    public async Task<SchedulingMutationOutcome<CourseDto>> UpdateContentAsync(
        Guid id, CourseContentRequest request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetForUpdateAsync(id, cancellationToken);
        if (course is null)
        {
            return Fail(SchedulingFailureKind.NotFound, "Course not found.");
        }

        if (!await courseRepository.CategoryExistsAsync(request.CategoryId, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Validation, "Select a valid category.");
        }

        var title = request.Title.Trim();
        var slug = SlugGenerator.Generate(title);
        if (await courseRepository.SlugExistsAsync(slug, excludedId: id, cancellationToken))
        {
            return Fail(SchedulingFailureKind.Conflict, "A course with a matching title already exists.");
        }

        course.CategoryId = request.CategoryId;
        course.Title = title;
        course.Slug = slug;
        course.Description = request.Description.Trim();
        course.Status = request.Status;
        course.Type = request.Type;

        await courseRepository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<CourseDto>.Success(
            (await courseRepository.GetByIdAsync(id, cancellationToken))!);
    }

    private static SchedulingMutationOutcome<CourseDto> Fail(SchedulingFailureKind kind, string error)
        => SchedulingMutationOutcome<CourseDto>.Failure(kind, error);
}
