namespace AndalusiaAcademy.Application.DTOs;

public sealed record CreateProgramRequest(
    string Title,
    string Description,
    string Status,
    decimal? Price,
    string? ImageUrl = null);
