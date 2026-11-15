namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record UpcomingSessionDto(
    Guid EnrollmentId,
    Guid SessionId,
    Guid? CourseOfferingId,
    Guid? ProgramOfferingId,
    string OfferingType,
    string OfferingTitle,
    string SessionTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string? CancellationReason,
    string? InstructorName,
    TrainingLocationDto? Location);
