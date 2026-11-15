namespace AndalusiaAcademy.Application.DTOs.Materials;

public sealed record CourseMaterialDto(
    Guid Id,
    Guid CourseId,
    Guid? CourseSessionId,
    string? SessionTitle,
    string Title,
    string? Description,
    string ResourceType,
    string? ResourceUrl,
    string? OriginalFileName,
    long? FileSizeBytes,
    string MaterialType,
    int SortOrder,
    string Visibility,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
