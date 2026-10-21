using AndalusiaAcademy.Application.DTOs.Progress;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class ProgressRepository(ApplicationDbContext dbContext, TimeProvider timeProvider) : IProgressRepository
{
    public async Task<bool> OfferingExistsAsync(
        Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken)
    {
        if (courseOfferingId.HasValue)
        {
            return await dbContext.CourseOfferings.AnyAsync(o => o.Id == courseOfferingId, cancellationToken);
        }

        return await dbContext.ProgramOfferings.AnyAsync(o => o.Id == programOfferingId, cancellationToken);
    }

    public async Task<bool> IsInstructorForOfferingAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken)
    {
        if (courseOfferingId.HasValue)
        {
            return await dbContext.CourseSessions.AnyAsync(
                session => session.CourseOfferingId == courseOfferingId && session.InstructorUserId == userId,
                cancellationToken);
        }

        return await dbContext.ProgramSessions.AnyAsync(
            session => session.ProgramOfferingId == programOfferingId && session.InstructorUserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<OfferingProgressRecordDto>> GetForOfferingAsync(
        Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var roster = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.Status == EnrollmentStatus.Active
                && (courseOfferingId == null || enrollment.CourseOfferingId == courseOfferingId)
                && (programOfferingId == null || enrollment.ProgramOfferingId == programOfferingId))
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

        if (roster.Count == 0)
        {
            return [];
        }

        var elapsedSessionIds = courseOfferingId.HasValue
            ? await dbContext.CourseSessions
                .AsNoTracking()
                .Where(session => session.CourseOfferingId == courseOfferingId
                    && session.Status != CourseSessionStatuses.Cancelled
                    && session.StartsAtUtc <= now)
                .Select(session => session.Id)
                .ToListAsync(cancellationToken)
            : await dbContext.ProgramSessions
                .AsNoTracking()
                .Where(session => session.ProgramOfferingId == programOfferingId
                    && session.Status != CourseSessionStatuses.Cancelled
                    && session.StartsAtUtc <= now)
                .Select(session => session.Id)
                .ToListAsync(cancellationToken);

        var presentCountsByUser = courseOfferingId.HasValue
            ? await dbContext.CourseSessionAttendances
                .AsNoTracking()
                .Where(record => elapsedSessionIds.Contains(record.CourseSessionId) && record.Status == AttendanceStatuses.Present)
                .GroupBy(record => record.UserId)
                .Select(group => new { UserId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.UserId, group => group.Count, cancellationToken)
            : await dbContext.ProgramSessionAttendances
                .AsNoTracking()
                .Where(record => elapsedSessionIds.Contains(record.ProgramSessionId) && record.Status == AttendanceStatuses.Present)
                .GroupBy(record => record.UserId)
                .Select(group => new { UserId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.UserId, group => group.Count, cancellationToken);

        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Where(assignment =>
                (courseOfferingId == null || assignment.CourseOfferingId == courseOfferingId) &&
                (programOfferingId == null || assignment.ProgramOfferingId == programOfferingId))
            .Select(assignment => new { assignment.Id, assignment.MaxScore })
            .ToListAsync(cancellationToken);

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var maxScoreById = assignments.ToDictionary(a => a.Id, a => a.MaxScore);

        var submissionsByUser = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Where(submission => assignmentIds.Contains(submission.AssignmentId))
            .Select(submission => new { submission.UserId, submission.AssignmentId, submission.Score })
            .ToListAsync(cancellationToken);

        var submissionsLookup = submissionsByUser
            .GroupBy(submission => submission.UserId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return roster
            .Select(learner =>
            {
                presentCountsByUser.TryGetValue(learner.UserId, out var attendedSessions);
                submissionsLookup.TryGetValue(learner.UserId, out var submissions);
                submissions ??= [];

                var gradedSubmissions = submissions.Where(s => s.Score is not null).ToList();
                var averageScorePercentage = gradedSubmissions.Count == 0
                    ? (decimal?)null
                    : gradedSubmissions.Average(s => s.Score!.Value / maxScoreById[s.AssignmentId] * 100m);

                return new OfferingProgressRecordDto(
                    learner.UserId,
                    learner.LearnerName,
                    learner.Email,
                    elapsedSessionIds.Count,
                    attendedSessions,
                    ComputeRatePercentage(attendedSessions, elapsedSessionIds.Count),
                    assignments.Count,
                    submissions.Count,
                    gradedSubmissions.Count,
                    averageScorePercentage,
                    ComputeOverallPercentage(
                        attendedSessions, elapsedSessionIds.Count, submissions.Count, assignments.Count));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<LearnerProgressDto>> GetForLearnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var enrollments = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new
            {
                enrollment.Id,
                enrollment.CourseOfferingId,
                CourseTitle = enrollment.CourseOffering != null ? enrollment.CourseOffering.Course!.Title : null,
                enrollment.ProgramOfferingId,
                ProgramTitle = enrollment.ProgramOffering != null ? enrollment.ProgramOffering.Program!.Title : null,
            })
            .ToListAsync(cancellationToken);

        var results = new List<LearnerProgressDto>(enrollments.Count);

        foreach (var enrollment in enrollments)
        {
            var elapsedSessionCount = enrollment.CourseOfferingId.HasValue
                ? await dbContext.CourseSessions.CountAsync(
                    session => session.CourseOfferingId == enrollment.CourseOfferingId
                        && session.Status != CourseSessionStatuses.Cancelled
                        && session.StartsAtUtc <= now,
                    cancellationToken)
                : await dbContext.ProgramSessions.CountAsync(
                    session => session.ProgramOfferingId == enrollment.ProgramOfferingId
                        && session.Status != CourseSessionStatuses.Cancelled
                        && session.StartsAtUtc <= now,
                    cancellationToken);

            var attendedSessionCount = enrollment.CourseOfferingId.HasValue
                ? await dbContext.CourseSessionAttendances.CountAsync(
                    record => record.UserId == userId
                        && record.Status == AttendanceStatuses.Present
                        && record.CourseSession!.CourseOfferingId == enrollment.CourseOfferingId
                        && record.CourseSession!.Status != CourseSessionStatuses.Cancelled
                        && record.CourseSession!.StartsAtUtc <= now,
                    cancellationToken)
                : await dbContext.ProgramSessionAttendances.CountAsync(
                    record => record.UserId == userId
                        && record.Status == AttendanceStatuses.Present
                        && record.ProgramSession!.ProgramOfferingId == enrollment.ProgramOfferingId
                        && record.ProgramSession!.Status != CourseSessionStatuses.Cancelled
                        && record.ProgramSession!.StartsAtUtc <= now,
                    cancellationToken)
                ;

            var assignments = await dbContext.Assignments
                .AsNoTracking()
                .Where(assignment =>
                    (enrollment.CourseOfferingId == null || assignment.CourseOfferingId == enrollment.CourseOfferingId) &&
                    (enrollment.ProgramOfferingId == null || assignment.ProgramOfferingId == enrollment.ProgramOfferingId))
                .Select(assignment => new { assignment.Id, assignment.MaxScore })
                .ToListAsync(cancellationToken);

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var maxScoreById = assignments.ToDictionary(a => a.Id, a => a.MaxScore);

            var submissions = await dbContext.AssignmentSubmissions
                .AsNoTracking()
                .Where(submission => submission.UserId == userId && assignmentIds.Contains(submission.AssignmentId))
                .Select(submission => new { submission.AssignmentId, submission.Score })
                .ToListAsync(cancellationToken);

            var gradedSubmissions = submissions.Where(s => s.Score is not null).ToList();
            var averageScorePercentage = gradedSubmissions.Count == 0
                ? (decimal?)null
                : gradedSubmissions.Average(s => s.Score!.Value / maxScoreById[s.AssignmentId] * 100m);

            results.Add(new LearnerProgressDto(
                enrollment.Id,
                enrollment.CourseOfferingId,
                enrollment.CourseTitle,
                enrollment.ProgramOfferingId,
                enrollment.ProgramTitle,
                elapsedSessionCount,
                attendedSessionCount,
                ComputeRatePercentage(attendedSessionCount, elapsedSessionCount),
                assignments.Count,
                submissions.Count,
                gradedSubmissions.Count,
                averageScorePercentage,
                ComputeOverallPercentage(attendedSessionCount, elapsedSessionCount, submissions.Count, assignments.Count)));
        }

        return results;
    }

    private static decimal ComputeRatePercentage(int numerator, int denominator)
        => denominator == 0 ? 0m : Math.Round(numerator / (decimal)denominator * 100m, 2);

    private static decimal ComputeOverallPercentage(
        int attendedSessions, int totalSessions, int submittedAssignments, int totalAssignments)
    {
        var rates = new List<decimal>();

        if (totalSessions > 0)
        {
            rates.Add(ComputeRatePercentage(attendedSessions, totalSessions));
        }

        if (totalAssignments > 0)
        {
            rates.Add(ComputeRatePercentage(submittedAssignments, totalAssignments));
        }

        return rates.Count == 0 ? 0m : Math.Round(rates.Average(), 2);
    }
}
