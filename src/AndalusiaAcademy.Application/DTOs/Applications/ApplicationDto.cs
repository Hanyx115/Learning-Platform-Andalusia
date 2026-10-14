namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record ApplicationDto(
    Guid Id,
    Guid UserId,
    Guid? CourseId,
    string? CourseTitle,
    Guid? ProgramId,
    string? ProgramTitle,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);
