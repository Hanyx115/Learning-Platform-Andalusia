namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record LearnerAssignmentDto(
    Guid AssignmentId,
    string Title,
    string? Description,
    Guid? CourseOfferingId,
    string? CourseTitle,
    Guid? ProgramOfferingId,
    string? ProgramTitle,
    DateTimeOffset DueAtUtc,
    decimal MaxScore,
    string? MySubmissionContent,
    string? MySubmissionType,
    string? MySubmissionLinkUrl,
    string? MySubmissionFileName,
    long? MySubmissionFileSizeBytes,
    DateTimeOffset? MySubmittedAtUtc,
    decimal? MyScore,
    string? MyFeedback);
