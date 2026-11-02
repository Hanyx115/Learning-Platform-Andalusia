namespace AndalusiaAcademy.Application.DTOs;

public sealed record CourseContentRequest(
    string Title,
    Guid CategoryId,
    string Description,
    string Status,
    string Type);
