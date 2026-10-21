namespace AndalusiaAcademy.Application.DTOs.Assignments;

public sealed record SubmissionConfirmationDto(Guid AssignmentId, string Content, DateTimeOffset SubmittedAtUtc);
