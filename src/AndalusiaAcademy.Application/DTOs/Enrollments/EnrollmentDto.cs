namespace AndalusiaAcademy.Application.DTOs.Enrollments;

public sealed record EnrollmentDto(
    Guid Id,
    Guid ApplicationId,
    Guid PaymentId,
    Guid? CourseId,
    string? CourseTitle,
    Guid? ProgramId,
    string? ProgramTitle,
    string Status,
    DateTimeOffset EnrolledAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt);
