using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseMaterialService(
    ICourseMaterialRepository repository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<CourseMaterialDto>?> GetForCourseManagementAsync(
        Guid courseId,
        Guid callerUserId,
        bool callerIsPrivileged,
        CancellationToken cancellationToken)
    {
        if (!await repository.CourseExistsAsync(courseId, cancellationToken))
        {
            return null;
        }

        if (!callerIsPrivileged && !await repository.IsInstructorForCourseAsync(callerUserId, courseId, cancellationToken))
        {
            return null;
        }

        return await repository.GetForCourseManagementAsync(courseId, cancellationToken);
    }

    public Task<IReadOnlyList<CourseMaterialDto>?> GetForLearnerEnrollmentAsync(
        Guid enrollmentId,
        Guid userId,
        CancellationToken cancellationToken)
        => repository.GetForLearnerEnrollmentAsync(enrollmentId, userId, cancellationToken);

    public async Task<CourseMaterialMutationOutcome> CreateAsync(
        Guid courseId,
        Guid createdByUserId,
        bool callerIsPrivileged,
        CreateCourseMaterialRequest request,
        CancellationToken cancellationToken)
    {
        if (!await repository.CourseExistsAsync(courseId, cancellationToken))
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course not found.");
        }

        if (!callerIsPrivileged && !await repository.IsInstructorForCourseAsync(createdByUserId, courseId, cancellationToken))
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course not found.");
        }

        var sessionFailure = await ValidateSessionAsync(
            request.CourseSessionId,
            courseId,
            cancellationToken);
        if (sessionFailure is not null)
        {
            return sessionFailure;
        }

        var material = new CourseMaterial
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            CourseSessionId = request.CourseSessionId,
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            ResourceUrl = request.ResourceUrl.Trim(),
            MaterialType = request.MaterialType,
            SortOrder = request.SortOrder,
            Visibility = request.Visibility,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        repository.Add(material);
        await repository.SaveChangesAsync(cancellationToken);

        return CourseMaterialMutationOutcome.Success(
            (await repository.GetByIdAsync(material.Id, cancellationToken))!);
    }

    public async Task<CourseMaterialMutationOutcome> UpdateAsync(
        Guid id,
        Guid callerUserId,
        bool callerIsPrivileged,
        UpdateCourseMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var material = await repository.GetForUpdateAsync(id, cancellationToken);
        if (material is null)
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material not found.");
        }

        if (!callerIsPrivileged && !await repository.IsInstructorForCourseAsync(callerUserId, material.CourseId, cancellationToken))
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material not found.");
        }

        var sessionFailure = await ValidateSessionAsync(
            request.CourseSessionId,
            material.CourseId,
            cancellationToken);
        if (sessionFailure is not null)
        {
            return sessionFailure;
        }

        material.CourseSessionId = request.CourseSessionId;
        material.Title = request.Title.Trim();
        material.Description = NormalizeOptional(request.Description);
        material.ResourceUrl = request.ResourceUrl.Trim();
        material.MaterialType = request.MaterialType;
        material.SortOrder = request.SortOrder;
        material.Visibility = request.Visibility;
        material.UpdatedAtUtc = timeProvider.GetUtcNow();

        await repository.SaveChangesAsync(cancellationToken);

        return CourseMaterialMutationOutcome.Success(
            (await repository.GetByIdAsync(material.Id, cancellationToken))!);
    }

    private async Task<CourseMaterialMutationOutcome?> ValidateSessionAsync(
        Guid? courseSessionId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (!courseSessionId.HasValue)
        {
            return null;
        }

        if (await repository.SessionBelongsToCourseAsync(
            courseSessionId.Value,
            courseId,
            cancellationToken))
        {
            return null;
        }

        return CourseMaterialMutationOutcome.Failure(
            CourseMaterialFailureKind.Validation,
            "The selected session does not belong to this course.");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
