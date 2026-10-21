namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record SchedulingConflictDto(
    Guid SessionId,
    string SessionType,
    string OfferingTitle,
    string SessionTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    Guid? LocationId,
    string? LocationName,
    Guid? InstructorUserId,
    string? InstructorName);
