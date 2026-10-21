using AndalusiaAcademy.Application.DTOs.Attendance;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class AttendanceService(
    IAttendanceRepository attendanceRepository,
    ISchedulingRepository schedulingRepository)
{
    public async Task<SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>> GetCourseSessionRosterAsync(
        Guid courseSessionId, Guid callerUserId, bool callerIsPrivileged, CancellationToken cancellationToken)
    {
        var session = await schedulingRepository.GetSessionForUpdateAsync(courseSessionId, cancellationToken);
        if (session is null || (!callerIsPrivileged && session.InstructorUserId != callerUserId))
        {
            // Same "not found" for missing vs. not-your-session, so this can't be used to probe
            // which sessions belong to another instructor.
            return Fail<IReadOnlyList<SessionAttendanceRecordDto>>(
                SchedulingFailureKind.NotFound, "Course session not found.");
        }

        var roster = await attendanceRepository.GetCourseSessionRosterAsync(courseSessionId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>.Success(roster);
    }

    public async Task<SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>> MarkCourseSessionAttendanceAsync(
        Guid courseSessionId,
        Guid callerUserId,
        bool callerIsPrivileged,
        MarkSessionAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var session = await schedulingRepository.GetSessionForUpdateAsync(courseSessionId, cancellationToken);
        if (session is null || (!callerIsPrivileged && session.InstructorUserId != callerUserId))
        {
            return Fail<IReadOnlyList<SessionAttendanceRecordDto>>(
                SchedulingFailureKind.NotFound, "Course session not found.");
        }

        var roster = await attendanceRepository.GetCourseSessionRosterAsync(courseSessionId, cancellationToken);
        var enrolledIds = roster.Select(record => record.UserId).ToHashSet();
        if (request.Entries.Any(entry => !enrolledIds.Contains(entry.UserId)))
        {
            return Fail<IReadOnlyList<SessionAttendanceRecordDto>>(
                SchedulingFailureKind.Validation, "One or more selected learners are not enrolled in this offering.");
        }

        await attendanceRepository.UpsertCourseSessionAttendanceAsync(
            courseSessionId, callerUserId, request.Entries, cancellationToken);
        var updated = await attendanceRepository.GetCourseSessionRosterAsync(courseSessionId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>.Success(updated);
    }

    public async Task<SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>> GetProgramSessionRosterAsync(
        Guid programSessionId, Guid callerUserId, bool callerIsPrivileged, CancellationToken cancellationToken)
    {
        var session = await schedulingRepository.GetProgramSessionForUpdateAsync(programSessionId, cancellationToken);
        if (session is null || (!callerIsPrivileged && session.InstructorUserId != callerUserId))
        {
            return Fail<IReadOnlyList<SessionAttendanceRecordDto>>(
                SchedulingFailureKind.NotFound, "Program session not found.");
        }

        var roster = await attendanceRepository.GetProgramSessionRosterAsync(programSessionId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>.Success(roster);
    }

    public async Task<SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>> MarkProgramSessionAttendanceAsync(
        Guid programSessionId,
        Guid callerUserId,
        bool callerIsPrivileged,
        MarkSessionAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var session = await schedulingRepository.GetProgramSessionForUpdateAsync(programSessionId, cancellationToken);
        if (session is null || (!callerIsPrivileged && session.InstructorUserId != callerUserId))
        {
            return Fail<IReadOnlyList<SessionAttendanceRecordDto>>(
                SchedulingFailureKind.NotFound, "Program session not found.");
        }

        var roster = await attendanceRepository.GetProgramSessionRosterAsync(programSessionId, cancellationToken);
        var enrolledIds = roster.Select(record => record.UserId).ToHashSet();
        if (request.Entries.Any(entry => !enrolledIds.Contains(entry.UserId)))
        {
            return Fail<IReadOnlyList<SessionAttendanceRecordDto>>(
                SchedulingFailureKind.Validation, "One or more selected learners are not enrolled in this offering.");
        }

        await attendanceRepository.UpsertProgramSessionAttendanceAsync(
            programSessionId, callerUserId, request.Entries, cancellationToken);
        var updated = await attendanceRepository.GetProgramSessionRosterAsync(programSessionId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>>.Success(updated);
    }

    public Task<IReadOnlyList<LearnerSessionAttendanceDto>> GetMyAttendanceAsync(
        Guid userId, CancellationToken cancellationToken)
        => attendanceRepository.GetForLearnerAsync(userId, cancellationToken);

    private static SchedulingMutationOutcome<T> Fail<T>(SchedulingFailureKind kind, string error)
        where T : class
        => SchedulingMutationOutcome<T>.Failure(kind, error);
}
