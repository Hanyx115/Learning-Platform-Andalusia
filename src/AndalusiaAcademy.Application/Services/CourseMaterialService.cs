using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseMaterialService(
    ICourseMaterialRepository repository,
    ICourseMaterialFileStore fileStore,
    TimeProvider timeProvider)
{
    private const long MaximumMaterialFileSizeBytes = 25 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedFileTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp",
            [".zip"] = "application/zip",
        };

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
            ResourceType = CourseMaterialResourceTypes.Link,
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

    public async Task<CourseMaterialMutationOutcome> CreateFileAsync(
        Guid courseId,
        Guid createdByUserId,
        bool callerIsPrivileged,
        CreateCourseMaterialFileRequest request,
        Stream content,
        string originalFileName,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(courseId, createdByUserId, callerIsPrivileged, cancellationToken))
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course not found.");
        }

        var sessionFailure = await ValidateSessionAsync(request.CourseSessionId, courseId, cancellationToken);
        if (sessionFailure is not null)
        {
            return sessionFailure;
        }

        var fileValidation = ValidateFile(originalFileName, fileSizeBytes);
        if (fileValidation.Error is not null)
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.Validation,
                fileValidation.Error);
        }

        var storedFile = await fileStore.StoreAsync(
            content,
            fileValidation.SafeFileName!,
            fileValidation.ContentType!,
            cancellationToken);

        if (storedFile.SizeBytes <= 0 || storedFile.SizeBytes > MaximumMaterialFileSizeBytes)
        {
            await fileStore.DeleteAsync(storedFile.StoredFileName, CancellationToken.None);
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.Validation,
                storedFile.SizeBytes <= 0
                    ? "Choose a non-empty file."
                    : "The file cannot exceed 25 MB.");
        }

        var material = new CourseMaterial
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            CourseSessionId = request.CourseSessionId,
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            ResourceType = CourseMaterialResourceTypes.File,
            ResourceUrl = null,
            OriginalFileName = storedFile.OriginalFileName,
            StoredFileName = storedFile.StoredFileName,
            FileContentType = storedFile.ContentType,
            FileSizeBytes = storedFile.SizeBytes,
            MaterialType = request.MaterialType,
            SortOrder = request.SortOrder,
            Visibility = request.Visibility,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        try
        {
            repository.Add(material);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileStore.DeleteAsync(storedFile.StoredFileName, CancellationToken.None);
            throw;
        }

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

        var previousStoredFileName = material.StoredFileName;
        ApplyMetadata(material, request.CourseSessionId, request.Title, request.Description,
            request.MaterialType, request.SortOrder, request.Visibility);
        material.ResourceType = CourseMaterialResourceTypes.Link;
        material.ResourceUrl = request.ResourceUrl.Trim();
        material.OriginalFileName = null;
        material.StoredFileName = null;
        material.FileContentType = null;
        material.FileSizeBytes = null;

        await repository.SaveChangesAsync(cancellationToken);
        await DeleteFileAsync(previousStoredFileName, cancellationToken);

        return CourseMaterialMutationOutcome.Success(
            (await repository.GetByIdAsync(material.Id, cancellationToken))!);
    }

    public async Task<CourseMaterialMutationOutcome> UpdateMetadataAsync(
        Guid id,
        Guid callerUserId,
        bool callerIsPrivileged,
        UpdateCourseMaterialMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var material = await repository.GetForUpdateAsync(id, cancellationToken);
        if (material is null || !await CanManageCourseAsync(
            material.CourseId, callerUserId, callerIsPrivileged, cancellationToken))
        {
            return CourseMaterialMutationOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material not found.");
        }

        var sessionFailure = await ValidateSessionAsync(request.CourseSessionId, material.CourseId, cancellationToken);
        if (sessionFailure is not null)
        {
            return sessionFailure;
        }

        ApplyMetadata(material, request.CourseSessionId, request.Title, request.Description,
            request.MaterialType, request.SortOrder, request.Visibility);
        await repository.SaveChangesAsync(cancellationToken);

        return CourseMaterialMutationOutcome.Success(
            (await repository.GetByIdAsync(material.Id, cancellationToken))!);
    }

    public async Task<CourseMaterialFileOutcome> GetStaffFileAsync(
        Guid materialId,
        Guid callerUserId,
        bool callerIsPrivileged,
        CancellationToken cancellationToken)
    {
        var material = await repository.GetForUpdateAsync(materialId, cancellationToken);
        if (material is null || !await CanManageCourseAsync(
            material.CourseId, callerUserId, callerIsPrivileged, cancellationToken))
        {
            return CourseMaterialFileOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material file not found.");
        }

        return await OpenFileAsync(material, cancellationToken);
    }

    public async Task<CourseMaterialFileOutcome> GetLearnerFileAsync(
        Guid enrollmentId,
        Guid materialId,
        Guid learnerUserId,
        CancellationToken cancellationToken)
    {
        var accessibleMaterials = await repository.GetForLearnerEnrollmentAsync(
            enrollmentId,
            learnerUserId,
            cancellationToken);
        if (accessibleMaterials is null || accessibleMaterials.All(material => material.Id != materialId))
        {
            return CourseMaterialFileOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material file not found.");
        }

        var material = await repository.GetForUpdateAsync(materialId, cancellationToken);
        return material is null
            ? CourseMaterialFileOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material file not found.")
            : await OpenFileAsync(material, cancellationToken);
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

    private async Task<bool> CanManageCourseAsync(
        Guid courseId,
        Guid callerUserId,
        bool callerIsPrivileged,
        CancellationToken cancellationToken)
        => await repository.CourseExistsAsync(courseId, cancellationToken)
            && (callerIsPrivileged || await repository.IsInstructorForCourseAsync(
                callerUserId, courseId, cancellationToken));

    private async Task<CourseMaterialFileOutcome> OpenFileAsync(
        CourseMaterial material,
        CancellationToken cancellationToken)
    {
        if (material.ResourceType != CourseMaterialResourceTypes.File ||
            string.IsNullOrWhiteSpace(material.StoredFileName) ||
            string.IsNullOrWhiteSpace(material.OriginalFileName) ||
            string.IsNullOrWhiteSpace(material.FileContentType))
        {
            return CourseMaterialFileOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material file not found.");
        }

        var stream = await fileStore.OpenReadAsync(material.StoredFileName, cancellationToken);
        return stream is null
            ? CourseMaterialFileOutcome.Failure(
                CourseMaterialFailureKind.NotFound,
                "Course material file not found.")
            : CourseMaterialFileOutcome.Success(
                new CourseMaterialFileDto(stream, material.FileContentType, material.OriginalFileName));
    }

    private static (string? SafeFileName, string? ContentType, string? Error) ValidateFile(
        string originalFileName,
        long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            return (null, null, "Choose a non-empty file.");
        }

        if (fileSizeBytes > MaximumMaterialFileSizeBytes)
        {
            return (null, null, "The file cannot exceed 25 MB.");
        }

        var safeFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeFileName);
        return string.IsNullOrWhiteSpace(safeFileName) || !AllowedFileTypes.TryGetValue(extension, out var contentType)
            ? (null, null, "Upload a PDF, Office document, text file, image, CSV, or ZIP archive.")
            : (safeFileName, contentType, null);
    }

    private void ApplyMetadata(
        CourseMaterial material,
        Guid? courseSessionId,
        string title,
        string? description,
        string materialType,
        int sortOrder,
        string visibility)
    {
        material.CourseSessionId = courseSessionId;
        material.Title = title.Trim();
        material.Description = NormalizeOptional(description);
        material.MaterialType = materialType;
        material.SortOrder = sortOrder;
        material.Visibility = visibility;
        material.UpdatedAtUtc = timeProvider.GetUtcNow();
    }

    private Task DeleteFileAsync(string? storedFileName, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(storedFileName)
            ? Task.CompletedTask
            : fileStore.DeleteAsync(storedFileName, cancellationToken);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
