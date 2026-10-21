namespace AndalusiaAcademy.Application.DTOs.Attendance;

/// <summary>One roster row per learner enrolled in the offering. Status/Note/RecordedAtUtc are
/// null when attendance has not been marked for that learner yet.</summary>
public sealed record SessionAttendanceRecordDto(
    Guid UserId,
    string LearnerName,
    string Email,
    string? Status,
    string? Note,
    Guid? RecordedByUserId,
    DateTimeOffset? RecordedAtUtc);
