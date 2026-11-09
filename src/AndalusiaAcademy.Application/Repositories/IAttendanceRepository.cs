using AndalusiaAcademy.Application.DTOs.Attendance;

namespace AndalusiaAcademy.Application.Repositories;

public interface IAttendanceRepository
{
    /// <summary>Every learner enrolled in the session's offering, with their current attendance
    /// status if already marked (null otherwise).</summary>
    Task<IReadOnlyList<SessionAttendanceRecordDto>> GetCourseSessionRosterAsync(
        Guid courseSessionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SessionAttendanceRecordDto>> GetProgramSessionRosterAsync(
        Guid programSessionId,
        CancellationToken cancellationToken);

    /// <summary>Creates or updates marked entries and removes an existing record when its status is empty.</summary>
    Task SaveCourseSessionAttendanceAsync(
        Guid courseSessionId,
        Guid recordedByUserId,
        IReadOnlyList<AttendanceEntryRequest> entries,
        CancellationToken cancellationToken);

    /// <summary>Creates or updates marked entries and removes an existing record when its status is empty.</summary>
    Task SaveProgramSessionAttendanceAsync(
        Guid programSessionId,
        Guid recordedByUserId,
        IReadOnlyList<AttendanceEntryRequest> entries,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearnerSessionAttendanceDto>> GetForLearnerAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
