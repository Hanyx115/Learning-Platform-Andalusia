namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record UpdateAssignmentRequest(
    string Title,
    string? Description,
    DateTimeOffset DueAtUtc,
    decimal MaxScore);
