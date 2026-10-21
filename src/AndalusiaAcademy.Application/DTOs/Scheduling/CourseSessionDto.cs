namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record CourseSessionDto(
    Guid Id,
    Guid CourseOfferingId,
    string CourseTitle,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string? CancellationReason,
    string? ReschedulingNote,
    Guid? InstructorUserId,
    string? InstructorName,
    TrainingLocationDto Location);
