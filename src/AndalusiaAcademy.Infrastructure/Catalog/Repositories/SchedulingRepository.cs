using System.Data;
using AndalusiaAcademy.Application.DTOs.Applications;
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
                IsolationLevel.Serializable, cancellationToken);
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
                location.Id, location.BranchName, location.RoomName, location.City,
                location.Address, location.MapUrl, location.Capacity, location.IsActive))
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
            location => location.Id != excludedId &&
                        location.BranchName == branchName &&
                        location.RoomName == roomName &&
                        location.City == city,
            cancellationToken);

    public async Task<int> GetMaximumOfferingCapacityForLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var courseMaximum = await dbContext.CourseOfferings
            .Where(offering => offering.LocationId == locationId)
            .Select(offering => (int?)offering.Capacity)
            .MaxAsync(cancellationToken) ?? 0;
        var programMaximum = await dbContext.ProgramOfferings
            .Where(offering => offering.LocationId == locationId)
            .Select(offering => (int?)offering.Capacity)
            .MaxAsync(cancellationToken) ?? 0;
        return Math.Max(courseMaximum, programMaximum);
    }

    public void AddLocation(TrainingLocation location) => dbContext.TrainingLocations.Add(location);

    public async Task<IReadOnlyList<CourseSessionDto>> GetSessionsForOfferingAsync(
        Guid courseOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CourseSessions.AsNoTracking()
            .Where(session => session.CourseOfferingId == courseOfferingId);
        if (!includeCancelled)
        {
            query = query.Where(session => session.Status != CourseSessionStatuses.Cancelled);
        }

        return await query.OrderBy(session => session.StartsAtUtc)
            .Select(session => new CourseSessionDto(
                session.Id,
                session.CourseOfferingId,
                session.CourseOffering!.Course!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.CancellationReason,
                session.ReschedulingNote,
                session.InstructorUserId,
                session.InstructorUserId == null ? null : dbContext.Users
                    .Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).SingleOrDefault(),
                session.Location == null ? null : new TrainingLocationDto(
                    session.Location.Id, session.Location.BranchName, session.Location.RoomName,
                    session.Location.City, session.Location.Address, session.Location.MapUrl,
                    session.Location.Capacity, session.Location.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProgramSessionDto>> GetSessionsForProgramOfferingAsync(
        Guid programOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProgramSessions.AsNoTracking()
            .Where(session => session.ProgramOfferingId == programOfferingId);
        if (!includeCancelled)
        {
            query = query.Where(session => session.Status != CourseSessionStatuses.Cancelled);
        }

        return await query.OrderBy(session => session.StartsAtUtc)
            .Select(session => new ProgramSessionDto(
                session.Id,
                session.ProgramOfferingId,
                session.ProgramOffering!.Program!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.CancellationReason,
                session.ReschedulingNote,
                session.InstructorUserId,
                session.InstructorUserId == null ? null : dbContext.Users
                    .Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).SingleOrDefault(),
                session.Location == null ? null : new TrainingLocationDto(
                    session.Location.Id, session.Location.BranchName, session.Location.RoomName,
                    session.Location.City, session.Location.Address, session.Location.MapUrl,
                    session.Location.Capacity, session.Location.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UpcomingSessionDto>> GetUpcomingSessionsForLearnerAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken)
    {
        var courseSessions = await (
            from session in dbContext.CourseSessions.AsNoTracking()
            join enrollment in dbContext.Enrollments.AsNoTracking()
                on session.CourseOfferingId equals enrollment.CourseOfferingId
            where enrollment.UserId == userId &&
                  enrollment.Status == "Active" &&
                  session.Status != CourseSessionStatuses.Completed &&
                  session.EndsAtUtc >= fromUtc
            select new UpcomingSessionDto(
                enrollment.Id,
                session.Id,
                session.CourseOfferingId,
                null,
                "Course",
                session.CourseOffering!.Course!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.CancellationReason,
                session.InstructorUserId == null ? null : dbContext.Users
                    .Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).SingleOrDefault(),
                session.Location == null ? null : new TrainingLocationDto(
                    session.Location.Id, session.Location.BranchName, session.Location.RoomName,
                    session.Location.City, session.Location.Address, session.Location.MapUrl,
                    session.Location.Capacity, session.Location.IsActive)))
            .ToListAsync(cancellationToken);

        var programSessions = await (
            from session in dbContext.ProgramSessions.AsNoTracking()
            join enrollment in dbContext.Enrollments.AsNoTracking()
                on session.ProgramOfferingId equals enrollment.ProgramOfferingId
            where enrollment.UserId == userId &&
                  enrollment.Status == "Active" &&
                  session.Status != CourseSessionStatuses.Completed &&
                  session.EndsAtUtc >= fromUtc
            select new UpcomingSessionDto(
                enrollment.Id,
                session.Id,
                null,
                session.ProgramOfferingId,
                "Program",
                session.ProgramOffering!.Program!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.CancellationReason,
                session.InstructorUserId == null ? null : dbContext.Users
                    .Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).SingleOrDefault(),
                session.Location == null ? null : new TrainingLocationDto(
                    session.Location.Id, session.Location.BranchName, session.Location.RoomName,
                    session.Location.City, session.Location.Address, session.Location.MapUrl,
                    session.Location.Capacity, session.Location.IsActive)))
            .ToListAsync(cancellationToken);

        return courseSessions.Concat(programSessions).OrderBy(session => session.StartsAtUtc).ToArray();
    }

    public Task<CourseOffering?> GetCourseOfferingForSchedulingAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CourseOfferings.SingleOrDefaultAsync(offering => offering.Id == id, cancellationToken);

    public Task<ProgramOffering?> GetProgramOfferingForSchedulingAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.ProgramOfferings.SingleOrDefaultAsync(offering => offering.Id == id, cancellationToken);

    public Task<OfferingDto?> GetCourseOfferingDetailsAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CourseOfferings.AsNoTracking().Where(offering => offering.Id == id)
            .Select(offering => new OfferingDto(
                offering.Id, offering.StartsAtUtc, offering.EndsAtUtc,
                offering.ApplicationOpensAtUtc, offering.ApplicationClosesAtUtc,
                offering.Price, offering.Currency, offering.Capacity, offering.Status,
                offering.Location == null ? null : new TrainingLocationDto(
                    offering.Location.Id, offering.Location.BranchName, offering.Location.RoomName,
                    offering.Location.City, offering.Location.Address, offering.Location.MapUrl,
                    offering.Location.Capacity, offering.Location.IsActive)))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<OfferingDto?> GetProgramOfferingDetailsAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.ProgramOfferings.AsNoTracking().Where(offering => offering.Id == id)
            .Select(offering => new OfferingDto(
                offering.Id, offering.StartsAtUtc, offering.EndsAtUtc,
                offering.ApplicationOpensAtUtc, offering.ApplicationClosesAtUtc,
                offering.Price, offering.Currency, offering.Capacity, offering.Status,
                offering.Location == null ? null : new TrainingLocationDto(
                    offering.Location.Id, offering.Location.BranchName, offering.Location.RoomName,
                    offering.Location.City, offering.Location.Address, offering.Location.MapUrl,
                    offering.Location.Capacity, offering.Location.IsActive)))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OfferingDto>> GetOfferingsForCourseAsync(Guid courseId, CancellationToken cancellationToken)
        => await dbContext.CourseOfferings.AsNoTracking()
            .Where(offering => offering.CourseId == courseId)
            .OrderByDescending(offering => offering.StartsAtUtc)
            .Select(offering => new OfferingDto(
                offering.Id, offering.StartsAtUtc, offering.EndsAtUtc,
                offering.ApplicationOpensAtUtc, offering.ApplicationClosesAtUtc,
                offering.Price, offering.Currency, offering.Capacity, offering.Status,
                offering.Location == null ? null : new TrainingLocationDto(
                    offering.Location.Id, offering.Location.BranchName, offering.Location.RoomName,
                    offering.Location.City, offering.Location.Address, offering.Location.MapUrl,
                    offering.Location.Capacity, offering.Location.IsActive)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OfferingDto>> GetOfferingsForProgramAsync(Guid programId, CancellationToken cancellationToken)
        => await dbContext.ProgramOfferings.AsNoTracking()
            .Where(offering => offering.ProgramId == programId)
            .OrderByDescending(offering => offering.StartsAtUtc)
            .Select(offering => new OfferingDto(
                offering.Id, offering.StartsAtUtc, offering.EndsAtUtc,
                offering.ApplicationOpensAtUtc, offering.ApplicationClosesAtUtc,
                offering.Price, offering.Currency, offering.Capacity, offering.Status,
                offering.Location == null ? null : new TrainingLocationDto(
                    offering.Location.Id, offering.Location.BranchName, offering.Location.RoomName,
                    offering.Location.City, offering.Location.Address, offering.Location.MapUrl,
                    offering.Location.Capacity, offering.Location.IsActive)))
            .ToListAsync(cancellationToken);

    public Task<bool> CourseExistsAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Courses.AnyAsync(course => course.Id == id, cancellationToken);

    public Task<bool> ProgramExistsAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Programs.AnyAsync(program => program.Id == id, cancellationToken);

    public Task<bool> CourseOfferingStartExistsAsync(Guid courseId, DateTimeOffset startsAtUtc, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.CourseOfferings.AnyAsync(offering => offering.CourseId == courseId && offering.StartsAtUtc == startsAtUtc && offering.Id != excludedId, cancellationToken);

    public Task<bool> ProgramOfferingStartExistsAsync(Guid programId, DateTimeOffset startsAtUtc, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.ProgramOfferings.AnyAsync(offering => offering.ProgramId == programId && offering.StartsAtUtc == startsAtUtc && offering.Id != excludedId, cancellationToken);

    public Task<bool> HasCourseSessionsOutsidePeriodAsync(Guid offeringId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(session => session.CourseOfferingId == offeringId && (session.StartsAtUtc < startsAtUtc || session.EndsAtUtc > endsAtUtc), cancellationToken);

    public Task<bool> CourseOfferingHasSessionsAsync(Guid offeringId, CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(session => session.CourseOfferingId == offeringId, cancellationToken);

    public Task<bool> HasProgramSessionsOutsidePeriodAsync(Guid offeringId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, CancellationToken cancellationToken)
        => dbContext.ProgramSessions.AnyAsync(session => session.ProgramOfferingId == offeringId && (session.StartsAtUtc < startsAtUtc || session.EndsAtUtc > endsAtUtc), cancellationToken);

    public Task<bool> ProgramOfferingHasSessionsAsync(Guid offeringId, CancellationToken cancellationToken)
        => dbContext.ProgramSessions.AnyAsync(session => session.ProgramOfferingId == offeringId, cancellationToken);

    public void AddCourseOffering(CourseOffering offering) => dbContext.CourseOfferings.Add(offering);

    public void AddProgramOffering(ProgramOffering offering) => dbContext.ProgramOfferings.Add(offering);

    public Task<CourseSession?> GetSessionForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CourseSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    public Task<ProgramSession?> GetProgramSessionForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.ProgramSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    public Task<bool> IsInstructorAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.UserRoles.AnyAsync(userRole => userRole.UserId == userId &&
            dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == RoleNames.Instructor), cancellationToken);

    public async Task<bool> HasLocationConflictAsync(
        Guid locationId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedCourseSessionId,
        Guid? excludedProgramSessionId,
        CancellationToken cancellationToken)
        => await dbContext.CourseSessions.AnyAsync(session =>
                session.LocationId == locationId && session.Id != excludedCourseSessionId &&
                session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc && startsAtUtc < session.EndsAtUtc, cancellationToken)
           || await dbContext.ProgramSessions.AnyAsync(session =>
                session.LocationId == locationId && session.Id != excludedProgramSessionId &&
                session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc && startsAtUtc < session.EndsAtUtc, cancellationToken);

    public async Task<bool> HasInstructorConflictAsync(
        Guid instructorUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedCourseSessionId,
        Guid? excludedProgramSessionId,
        CancellationToken cancellationToken)
        => await dbContext.CourseSessions.AnyAsync(session =>
                session.InstructorUserId == instructorUserId && session.Id != excludedCourseSessionId &&
                session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc && startsAtUtc < session.EndsAtUtc, cancellationToken)
           || await dbContext.ProgramSessions.AnyAsync(session =>
                session.InstructorUserId == instructorUserId && session.Id != excludedProgramSessionId &&
                session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc && startsAtUtc < session.EndsAtUtc, cancellationToken);

    public Task<bool> CourseSessionStartExistsAsync(Guid offeringId, DateTimeOffset startsAtUtc, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(session => session.CourseOfferingId == offeringId && session.StartsAtUtc == startsAtUtc && session.Id != excludedId, cancellationToken);

    public Task<bool> ProgramSessionStartExistsAsync(Guid offeringId, DateTimeOffset startsAtUtc, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.ProgramSessions.AnyAsync(session => session.ProgramOfferingId == offeringId && session.StartsAtUtc == startsAtUtc && session.Id != excludedId, cancellationToken);

    public async Task<IReadOnlyList<SchedulingConflictDto>> GetConflictsAsync(
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? locationId,
        Guid? instructorUserId,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.CourseSessions.AsNoTracking()
            .Where(session => session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc && startsAtUtc < session.EndsAtUtc &&
                (!locationId.HasValue || session.LocationId == locationId) &&
                (!instructorUserId.HasValue || session.InstructorUserId == instructorUserId))
            .Select(session => new SchedulingConflictDto(
                session.Id, "Course", session.CourseOffering!.Course!.Title, session.Title,
                session.StartsAtUtc, session.EndsAtUtc, session.LocationId,
                session.Location == null ? null : session.Location.BranchName + " - " + session.Location.RoomName,
                session.InstructorUserId,
                session.InstructorUserId == null ? null : dbContext.Users
                    .Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).SingleOrDefault()))
            .ToListAsync(cancellationToken);

        var program = await dbContext.ProgramSessions.AsNoTracking()
            .Where(session => session.Status == CourseSessionStatuses.Scheduled &&
                session.StartsAtUtc < endsAtUtc && startsAtUtc < session.EndsAtUtc &&
                (!locationId.HasValue || session.LocationId == locationId) &&
                (!instructorUserId.HasValue || session.InstructorUserId == instructorUserId))
            .Select(session => new SchedulingConflictDto(
                session.Id, "Program", session.ProgramOffering!.Program!.Title, session.Title,
                session.StartsAtUtc, session.EndsAtUtc, session.LocationId,
                session.Location == null ? null : session.Location.BranchName + " - " + session.Location.RoomName,
                session.InstructorUserId,
                session.InstructorUserId == null ? null : dbContext.Users
                    .Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).SingleOrDefault()))
            .ToListAsync(cancellationToken);

        return course.Concat(program).OrderBy(item => item.StartsAtUtc).ToArray();
    }

    public void AddSession(CourseSession session) => dbContext.CourseSessions.Add(session);

    public void AddProgramSession(ProgramSession session) => dbContext.ProgramSessions.Add(session);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);

    private static TrainingLocationDto MapLocation(TrainingLocation location)
        => new(location.Id, location.BranchName, location.RoomName, location.City,
            location.Address, location.MapUrl, location.Capacity, location.IsActive);

}
