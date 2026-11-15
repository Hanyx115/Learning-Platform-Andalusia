namespace AndalusiaAcademy.Application.DTOs.Attendance;

public sealed record LearnerSessionAttendanceDto(
    Guid SessionId,
    string SessionTitle,
    string? CourseTitle,
    string? ProgramTitle,
    DateTimeOffset StartsAtUtc,
    string Status,
    string? Note);
