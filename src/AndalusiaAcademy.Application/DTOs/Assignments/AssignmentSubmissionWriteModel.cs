namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record AssignmentSubmissionWriteModel(
    string Content,
    string SubmissionType,
    string? LinkUrl,
    string? OriginalFileName,
    string? StoredFileName,
    string? FileContentType,
    long? FileSizeBytes);
