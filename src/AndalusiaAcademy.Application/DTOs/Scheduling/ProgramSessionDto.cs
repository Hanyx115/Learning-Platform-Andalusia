namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record ProgramSessionDto(
    Guid Id,
    Guid ProgramOfferingId,
    string ProgramTitle,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string? CancellationReason,
    string? ReschedulingNote,
    Guid? InstructorUserId,
    string? InstructorName,
    TrainingLocationDto? Location);
