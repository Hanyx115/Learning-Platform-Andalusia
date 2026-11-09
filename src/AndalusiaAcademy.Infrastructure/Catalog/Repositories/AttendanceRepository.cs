using AndalusiaAcademy.Application.DTOs.Attendance;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class AttendanceRepository(ApplicationDbContext dbContext) : IAttendanceRepository
{
    public async Task<IReadOnlyList<SessionAttendanceRecordDto>> GetCourseSessionRosterAsync(
        Guid courseSessionId,
        CancellationToken cancellationToken)
    {
        var courseOfferingId = await dbContext.CourseSessions
            .AsNoTracking()
            .Where(session => session.Id == courseSessionId)
            .Select(session => (Guid?)session.CourseOfferingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (courseOfferingId is null)
        {
            return [];
        }

        var roster = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.CourseOfferingId == courseOfferingId && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new
            {
                enrollment.UserId,
                LearnerName = dbContext.Users
                    .Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName)
                    .Single(),
                Email = dbContext.Users
                    .Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.Email!)
                    .Single(),
            })
            .ToListAsync(cancellationToken);

        var attendance = await dbContext.CourseSessionAttendances
            .AsNoTracking()
            .Where(record => record.CourseSessionId == courseSessionId)
            .ToDictionaryAsync(record => record.UserId, cancellationToken);

        return roster
            .Select(learner =>
            {
                attendance.TryGetValue(learner.UserId, out var record);
                return new SessionAttendanceRecordDto(
                    learner.UserId, learner.LearnerName, learner.Email,
                    record?.Status, record?.Note, record?.RecordedByUserId, record?.RecordedAtUtc);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SessionAttendanceRecordDto>> GetProgramSessionRosterAsync(
        Guid programSessionId,
        CancellationToken cancellationToken)
    {
        var programOfferingId = await dbContext.ProgramSessions
            .AsNoTracking()
            .Where(session => session.Id == programSessionId)
            .Select(session => (Guid?)session.ProgramOfferingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (programOfferingId is null)
        {
            return [];
        }

        var roster = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.ProgramOfferingId == programOfferingId && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new
            {
                enrollment.UserId,
                LearnerName = dbContext.Users
                    .Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName)
                    .Single(),
                Email = dbContext.Users
                    .Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.Email!)
                    .Single(),
            })
            .ToListAsync(cancellationToken);

        var attendance = await dbContext.ProgramSessionAttendances
            .AsNoTracking()
            .Where(record => record.ProgramSessionId == programSessionId)
            .ToDictionaryAsync(record => record.UserId, cancellationToken);

        return roster
            .Select(learner =>
            {
                attendance.TryGetValue(learner.UserId, out var record);
                return new SessionAttendanceRecordDto(
                    learner.UserId, learner.LearnerName, learner.Email,
                    record?.Status, record?.Note, record?.RecordedByUserId, record?.RecordedAtUtc);
            })
            .ToList();
    }

    public async Task SaveCourseSessionAttendanceAsync(
        Guid courseSessionId,
        Guid recordedByUserId,
        IReadOnlyList<AttendanceEntryRequest> entries,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var userIds = entries.Select(entry => entry.UserId).ToList();
        var existing = await dbContext.CourseSessionAttendances
            .Where(record => record.CourseSessionId == courseSessionId && userIds.Contains(record.UserId))
            .ToDictionaryAsync(record => record.UserId, cancellationToken);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Status))
            {
                if (existing.TryGetValue(entry.UserId, out var recordToRemove))
                {
                    dbContext.CourseSessionAttendances.Remove(recordToRemove);
                }

                continue;
            }

            if (existing.TryGetValue(entry.UserId, out var record))
            {
                record.Status = entry.Status;
                record.Note = NormalizeNote(entry.Note);
                record.RecordedByUserId = recordedByUserId;
                record.UpdatedAtUtc = now;
            }
            else
            {
                dbContext.CourseSessionAttendances.Add(new CourseSessionAttendance
                {
                    Id = Guid.NewGuid(),
                    CourseSessionId = courseSessionId,
                    UserId = entry.UserId,
                    Status = entry.Status,
                    Note = NormalizeNote(entry.Note),
                    RecordedByUserId = recordedByUserId,
                    RecordedAtUtc = now,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveProgramSessionAttendanceAsync(
        Guid programSessionId,
        Guid recordedByUserId,
        IReadOnlyList<AttendanceEntryRequest> entries,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var userIds = entries.Select(entry => entry.UserId).ToList();
        var existing = await dbContext.ProgramSessionAttendances
            .Where(record => record.ProgramSessionId == programSessionId && userIds.Contains(record.UserId))
            .ToDictionaryAsync(record => record.UserId, cancellationToken);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Status))
            {
                if (existing.TryGetValue(entry.UserId, out var recordToRemove))
                {
                    dbContext.ProgramSessionAttendances.Remove(recordToRemove);
                }

                continue;
            }

            if (existing.TryGetValue(entry.UserId, out var record))
            {
                record.Status = entry.Status;
                record.Note = NormalizeNote(entry.Note);
                record.RecordedByUserId = recordedByUserId;
                record.UpdatedAtUtc = now;
            }
            else
            {
                dbContext.ProgramSessionAttendances.Add(new ProgramSessionAttendance
                {
                    Id = Guid.NewGuid(),
                    ProgramSessionId = programSessionId,
                    UserId = entry.UserId,
                    Status = entry.Status,
                    Note = NormalizeNote(entry.Note),
                    RecordedByUserId = recordedByUserId,
                    RecordedAtUtc = now,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LearnerSessionAttendanceDto>> GetForLearnerAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var courseRecords = await dbContext.CourseSessionAttendances
            .AsNoTracking()
            .Where(record => record.UserId == userId)
            .Select(record => new LearnerSessionAttendanceDto(
                record.CourseSessionId,
                record.CourseSession!.Title,
                record.CourseSession!.CourseOffering!.Course!.Title,
                null,
                record.CourseSession!.StartsAtUtc,
                record.Status,
                record.Note))
            .ToListAsync(cancellationToken);

        var programRecords = await dbContext.ProgramSessionAttendances
            .AsNoTracking()
            .Where(record => record.UserId == userId)
            .Select(record => new LearnerSessionAttendanceDto(
                record.ProgramSessionId,
                record.ProgramSession!.Title,
                null,
                record.ProgramSession!.ProgramOffering!.Program!.Title,
                record.ProgramSession!.StartsAtUtc,
                record.Status,
                record.Note))
            .ToListAsync(cancellationToken);

        return courseRecords
            .Concat(programRecords)
            .OrderByDescending(record => record.StartsAtUtc)
            .ToList();
    }

    private static string? NormalizeNote(string? note)
        => string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
