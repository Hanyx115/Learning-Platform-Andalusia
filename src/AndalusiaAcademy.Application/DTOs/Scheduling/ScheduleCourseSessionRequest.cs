namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record ScheduleCourseSessionRequest(
    Guid? LocationId,
    Guid InstructorUserId,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);
