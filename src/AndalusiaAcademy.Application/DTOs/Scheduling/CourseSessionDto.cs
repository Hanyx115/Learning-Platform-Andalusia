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
    Guid? InstructorUserId,
    string? InstructorName,
    TrainingLocationDto Location);
