namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record CreateAssignmentRequest(
    string Title,
    string? Description,
    DateTimeOffset DueAtUtc,
    decimal MaxScore);
