namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record RescheduleCourseSessionRequest(
    Guid? LocationId,
    Guid InstructorUserId,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Note);
