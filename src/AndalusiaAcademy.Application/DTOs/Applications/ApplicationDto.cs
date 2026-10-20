namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record ApplicationDto(
    Guid Id,
    Guid UserId,
    Guid? CourseOfferingId,
    Guid? CourseId,
    string? CourseTitle,
    Guid? ProgramOfferingId,
    Guid? ProgramId,
    string? ProgramTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    decimal Price,
    string Currency,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    IReadOnlyList<ApplicationStatusHistoryDto> StatusHistory);
