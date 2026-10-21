namespace AndalusiaAcademy.Application.DTOs.Assignments;

/// <summary>One roster row per learner enrolled in the assignment's offering. Submission fields
/// are null when the learner has not submitted yet.</summary>
public sealed record SubmissionRecordDto(
    Guid UserId,
    string LearnerName,
    string Email,
    string? Content,
    DateTimeOffset? SubmittedAtUtc,
    decimal? Score,
    string? Feedback,
    DateTimeOffset? GradedAtUtc);
