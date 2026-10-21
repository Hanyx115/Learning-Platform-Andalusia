using AndalusiaAcademy.Application.DTOs.Scheduling;
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

    void AddLocation(TrainingLocation location);

    Task<IReadOnlyList<CourseSessionDto>> GetSessionsForOfferingAsync(
        Guid courseOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UpcomingSessionDto>> GetUpcomingSessionsForLearnerAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken);

    Task<CourseOffering?> GetCourseOfferingForSchedulingAsync(
        Guid courseOfferingId,
        CancellationToken cancellationToken);

    Task<CourseSession?> GetSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<bool> IsInstructorAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> HasLocationConflictAsync(
        Guid locationId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken);

    Task<bool> HasInstructorConflictAsync(
        Guid instructorUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken);

    void AddSession(CourseSession session);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
