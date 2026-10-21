using System.Data;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class SchedulingRepository(ApplicationDbContext dbContext) : ISchedulingRepository
{
    public async Task<TResult> ExecuteSerializableAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public async Task<IReadOnlyList<TrainingLocationDto>> GetLocationsAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var locations = dbContext.TrainingLocations.AsNoTracking();

        if (!includeInactive)
        {
            locations = locations.Where(location => location.IsActive);
        }

        return await locations
            .OrderBy(location => location.City)
            .ThenBy(location => location.BranchName)
            .ThenBy(location => location.RoomName)
            .Select(location => new TrainingLocationDto(
                location.Id,
                location.BranchName,
                location.RoomName,
                location.City,
                location.Address,
                location.MapUrl,
                location.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<TrainingLocation?> GetLocationForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.TrainingLocations.SingleOrDefaultAsync(location => location.Id == id, cancellationToken);

    public Task<bool> LocationNameExistsAsync(
        string branchName,
        string roomName,
        string city,
        Guid? excludedId,
        CancellationToken cancellationToken)
        => dbContext.TrainingLocations.AnyAsync(
            location =>
                location.Id != excludedId &&
                location.BranchName == branchName &&
                location.RoomName == roomName &&
                location.City == city,
            cancellationToken);

    public void AddLocation(TrainingLocation location) => dbContext.TrainingLocations.Add(location);

    public async Task<IReadOnlyList<CourseSessionDto>> GetSessionsForOfferingAsync(
        Guid courseOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var sessions = dbContext.CourseSessions
            .AsNoTracking()
            .Where(session => session.CourseOfferingId == courseOfferingId);

        if (!includeCancelled)
        {
            sessions = sessions.Where(session => session.Status != CourseSessionStatuses.Cancelled);
        }

        return await sessions
            .OrderBy(session => session.StartsAtUtc)
            .Select(session => new CourseSessionDto(
                session.Id,
                session.CourseOfferingId,
                session.CourseOffering!.Course!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.CancellationReason,
                session.InstructorUserId,
                session.InstructorUserId == null
                    ? null
                    : dbContext.Users
                        .Where(user => user.Id == session.InstructorUserId)
                        .Select(user => user.FirstName + " " + user.LastName)
                        .SingleOrDefault(),
                new TrainingLocationDto(
                    session.Location!.Id,
                    session.Location.BranchName,
                    session.Location.RoomName,
                    session.Location.City,
                    session.Location.Address,
                    session.Location.MapUrl,
                    session.Location.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UpcomingSessionDto>> GetUpcomingSessionsForLearnerAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken)
    {
        return await (
            from session in dbContext.CourseSessions.AsNoTracking()
            join enrollment in dbContext.Enrollments.AsNoTracking()
                on session.CourseOfferingId equals enrollment.CourseOfferingId
            where enrollment.UserId == userId &&
                  enrollment.Status == "Active" &&
                  session.Status == CourseSessionStatuses.Scheduled &&
                  session.EndsAtUtc >= fromUtc
            orderby session.StartsAtUtc
            select new UpcomingSessionDto(
                enrollment.Id,
                session.Id,
                session.CourseOfferingId,
                session.CourseOffering!.Course!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.InstructorUserId == null
                    ? null
                    : dbContext.Users
                        .Where(user => user.Id == session.InstructorUserId)
                        .Select(user => user.FirstName + " " + user.LastName)
                        .SingleOrDefault(),
                new TrainingLocationDto(
                    session.Location!.Id,
                    session.Location.BranchName,
                    session.Location.RoomName,
                    session.Location.City,
                    session.Location.Address,
                    session.Location.MapUrl,
                    session.Location.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public Task<CourseOffering?> GetCourseOfferingForSchedulingAsync(
        Guid courseOfferingId,
        CancellationToken cancellationToken)
        => dbContext.CourseOfferings.SingleOrDefaultAsync(
            offering => offering.Id == courseOfferingId,
            cancellationToken);

    public Task<CourseSession?> GetSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken)
        => dbContext.CourseSessions.SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public Task<bool> IsInstructorAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.UserRoles.AnyAsync(
            userRole => userRole.UserId == userId &&
                        dbContext.Roles.Any(
                            role => role.Id == userRole.RoleId && role.Name == RoleNames.Instructor),
            cancellationToken);

    public Task<bool> HasLocationConflictAsync(
        Guid locationId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(
            session =>
                session.LocationId == locationId &&
                session.Id != excludedSessionId &&
                session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc &&
                startsAtUtc < session.EndsAtUtc,
            cancellationToken);

    public Task<bool> HasInstructorConflictAsync(
        Guid instructorUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(
            session =>
                session.InstructorUserId == instructorUserId &&
                session.Id != excludedSessionId &&
                session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc &&
                startsAtUtc < session.EndsAtUtc,
            cancellationToken);

    public void AddSession(CourseSession session) => dbContext.CourseSessions.Add(session);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);
}
