namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record AssignmentSubmissionFileDto(
    Stream Content,
    string ContentType,
    string OriginalFileName);
