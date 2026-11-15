namespace AndalusiaAcademy.Application.DTOs;

public sealed record UpdateCourseRequest(
    string Title,
    Guid CategoryId,
    string Description,
    string Status,
    string Type,
    decimal? Price,
    string? ImageUrl = null);
