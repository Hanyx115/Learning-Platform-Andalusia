namespace AndalusiaAcademy.Application.DTOs.Materials;

public sealed record CreateCourseMaterialFileRequest(
    Guid? CourseSessionId,
    string Title,
    string? Description,
    string MaterialType,
    int SortOrder,
    string Visibility);
