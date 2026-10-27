namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record SubmissionConfirmationDto(
    Guid AssignmentId,
    string Content,
    string SubmissionType,
    string? LinkUrl,
    string? OriginalFileName,
    long? FileSizeBytes,
    DateTimeOffset SubmittedAtUtc);
