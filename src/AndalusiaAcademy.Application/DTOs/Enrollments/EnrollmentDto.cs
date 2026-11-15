using AndalusiaAcademy.Application.DTOs.Scheduling;

namespace AndalusiaAcademy.Application.DTOs.Enrollments;

public sealed record EnrollmentDto(
    Guid Id,
    Guid ApplicationId,
    Guid PaymentId,
    Guid? CourseOfferingId,
    Guid? CourseId,
    string? CourseTitle,
    Guid? ProgramOfferingId,
    Guid? ProgramId,
    string? ProgramTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    TrainingLocationDto? Location,
    string Status,
    DateTimeOffset EnrolledAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt);
