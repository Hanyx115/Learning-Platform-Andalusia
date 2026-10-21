namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record UpcomingSessionDto(
    Guid EnrollmentId,
    Guid SessionId,
    Guid CourseOfferingId,
    string CourseTitle,
    string SessionTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? InstructorName,
    TrainingLocationDto Location);
