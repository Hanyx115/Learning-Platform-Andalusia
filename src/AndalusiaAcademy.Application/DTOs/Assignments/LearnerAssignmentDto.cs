namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record LearnerAssignmentDto(
    Guid AssignmentId,
    string Title,
    string? Description,
    string? CourseTitle,
    string? ProgramTitle,
    DateTimeOffset DueAtUtc,
    decimal MaxScore,
    string? MySubmissionContent,
    DateTimeOffset? MySubmittedAtUtc,
    decimal? MyScore,
    string? MyFeedback);
