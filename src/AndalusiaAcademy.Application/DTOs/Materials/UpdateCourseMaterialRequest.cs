namespace AndalusiaAcademy.Application.DTOs.Materials;

public sealed record UpdateCourseMaterialRequest(
    Guid? CourseSessionId,
    string Title,
    string? Description,
    string ResourceUrl,
    string MaterialType,
    int SortOrder,
    string Visibility);
