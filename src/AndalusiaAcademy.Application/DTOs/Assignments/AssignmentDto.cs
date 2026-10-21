namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record AssignmentDto(
    Guid Id,
    Guid? CourseOfferingId,
    Guid? ProgramOfferingId,
    string Title,
    string? Description,
    DateTimeOffset DueAtUtc,
    decimal MaxScore,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
