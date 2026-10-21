using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ISchedulingRepository
{
    Task<TResult> ExecuteSerializableAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TrainingLocationDto>> GetLocationsAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<TrainingLocation?> GetLocationForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> LocationNameExistsAsync(
        string branchName,
        string roomName,
        string city,
        Guid? excludedId,
        CancellationToken cancellationToken);

    Task<int> GetMaximumOfferingCapacityForLocationAsync(Guid locationId, CancellationToken cancellationToken);

    void AddLocation(TrainingLocation location);

    Task<IReadOnlyList<CourseSessionDto>> GetSessionsForOfferingAsync(
        Guid courseOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProgramSessionDto>> GetSessionsForProgramOfferingAsync(
        Guid programOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UpcomingSessionDto>> GetUpcomingSessionsForLearnerAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken);

    Task<CourseOffering?> GetCourseOfferingForSchedulingAsync(
        Guid courseOfferingId,
        CancellationToken cancellationToken);

    Task<ProgramOffering?> GetProgramOfferingForSchedulingAsync(
        Guid programOfferingId,
        CancellationToken cancellationToken);

    Task<OfferingDto?> GetCourseOfferingDetailsAsync(Guid offeringId, CancellationToken cancellationToken);

    Task<OfferingDto?> GetProgramOfferingDetailsAsync(Guid offeringId, CancellationToken cancellationToken);

    Task<bool> CourseExistsAsync(Guid courseId, CancellationToken cancellationToken);

    Task<bool> ProgramExistsAsync(Guid programId, CancellationToken cancellationToken);

    Task<bool> CourseOfferingStartExistsAsync(
        Guid courseId,
        DateTimeOffset startsAtUtc,
        Guid? excludedOfferingId,
        CancellationToken cancellationToken);

    Task<bool> ProgramOfferingStartExistsAsync(
        Guid programId,
        DateTimeOffset startsAtUtc,
        Guid? excludedOfferingId,
        CancellationToken cancellationToken);

    Task<bool> HasCourseSessionsOutsidePeriodAsync(
        Guid offeringId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken);

    Task<bool> CourseOfferingHasSessionsAsync(Guid offeringId, CancellationToken cancellationToken);

    Task<bool> HasProgramSessionsOutsidePeriodAsync(
        Guid offeringId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken);

    Task<bool> ProgramOfferingHasSessionsAsync(Guid offeringId, CancellationToken cancellationToken);

    void AddCourseOffering(CourseOffering offering);

    void AddProgramOffering(ProgramOffering offering);

    Task<CourseSession?> GetSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<ProgramSession?> GetProgramSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<bool> IsInstructorAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> HasLocationConflictAsync(
        Guid locationId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedCourseSessionId,
        Guid? excludedProgramSessionId,
        CancellationToken cancellationToken);

    Task<bool> HasInstructorConflictAsync(
        Guid instructorUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedCourseSessionId,
        Guid? excludedProgramSessionId,
        CancellationToken cancellationToken);

    Task<bool> CourseSessionStartExistsAsync(
        Guid offeringId,
        DateTimeOffset startsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken);

    Task<bool> ProgramSessionStartExistsAsync(
        Guid offeringId,
        DateTimeOffset startsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SchedulingConflictDto>> GetConflictsAsync(
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? locationId,
        Guid? instructorUserId,
        CancellationToken cancellationToken);

    void AddSession(CourseSession session);

    void AddProgramSession(ProgramSession session);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
