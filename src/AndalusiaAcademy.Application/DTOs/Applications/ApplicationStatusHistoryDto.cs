namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record ApplicationStatusHistoryDto(
    Guid Id,
    string Status,
    DateTimeOffset ChangedAt,
    string? Note);
