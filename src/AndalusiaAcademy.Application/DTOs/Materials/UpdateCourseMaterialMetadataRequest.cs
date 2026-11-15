namespace AndalusiaAcademy.Application.DTOs.Materials;

public sealed record UpdateCourseMaterialMetadataRequest(
    Guid? CourseSessionId,
    string Title,
    string? Description,
    string MaterialType,
    int SortOrder,
    string Visibility);
