namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record StoredAssignmentSubmissionFile(
    string StoredFileName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);
