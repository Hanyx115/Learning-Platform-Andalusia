using AndalusiaAcademy.Application.DTOs.Scheduling;

namespace AndalusiaAcademy.Application.DTOs.Enrollments;

/// <summary>The admin roster view of an enrollment: same fields as EnrollmentDto plus who it
/// belongs to, since an admin is looking across every learner rather than their own.</summary>
public sealed record AdminEnrollmentDto(
    Guid Id,
    Guid UserId,
    string LearnerName,
    string Email,
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
