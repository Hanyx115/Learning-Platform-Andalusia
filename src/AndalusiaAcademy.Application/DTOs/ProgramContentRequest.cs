namespace AndalusiaAcademy.Application.DTOs;

public sealed record ProgramContentRequest(
    string Title,
    string Description,
    string Status,
    string? ImageUrl = null);
