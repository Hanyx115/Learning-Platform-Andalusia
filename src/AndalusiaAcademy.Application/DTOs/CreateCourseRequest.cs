namespace AndalusiaAcademy.Application.DTOs;

public sealed record CreateCourseRequest(
    string Title,
    Guid CategoryId,
    string Description,
    string Status,
    string Type,
    decimal? Price);
