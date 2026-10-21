using System.Linq.Expressions;
using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class AssignmentRepository(ApplicationDbContext dbContext) : IAssignmentRepository
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

    public async Task<bool> IsEnrolledAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken)
    {
        return await dbContext.Enrollments.AnyAsync(
            enrollment => enrollment.UserId == userId
                && enrollment.Status == EnrollmentStatus.Active
                && (courseOfferingId == null || enrollment.CourseOfferingId == courseOfferingId)
                && (programOfferingId == null || enrollment.ProgramOfferingId == programOfferingId),
            cancellationToken);
    }

    public void Add(Assignment assignment) => dbContext.Assignments.Add(assignment);

    public Task<Assignment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Assignments.FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public Task<AssignmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Assignments.AsNoTracking()
            .Where(assignment => assignment.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AssignmentDto>> GetForOfferingAsync(
        Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken)
    {
        return await dbContext.Assignments
            .AsNoTracking()
            .Where(assignment =>
                (courseOfferingId == null || assignment.CourseOfferingId == courseOfferingId) &&
                (programOfferingId == null || assignment.ProgramOfferingId == programOfferingId))
            .OrderBy(assignment => assignment.DueAtUtc)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubmissionRecordDto>> GetRosterAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Where(a => a.Id == assignmentId)
            .Select(a => new { a.CourseOfferingId, a.ProgramOfferingId })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return [];
        }

        var roster = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.Status == EnrollmentStatus.Active
                && (assignment.CourseOfferingId == null || enrollment.CourseOfferingId == assignment.CourseOfferingId)
                && (assignment.ProgramOfferingId == null || enrollment.ProgramOfferingId == assignment.ProgramOfferingId))
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

        var submissions = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Where(submission => submission.AssignmentId == assignmentId)
            .ToDictionaryAsync(submission => submission.UserId, cancellationToken);

        return roster
            .Select(learner =>
            {
                submissions.TryGetValue(learner.UserId, out var submission);
                return new SubmissionRecordDto(
                    learner.UserId, learner.LearnerName, learner.Email,
                    submission?.Content, submission?.SubmittedAtUtc,
                    submission?.Score, submission?.Feedback, submission?.GradedAtUtc);
            })
            .ToList();
    }

    public Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid userId, CancellationToken cancellationToken)
        => dbContext.AssignmentSubmissions.FirstOrDefaultAsync(
            submission => submission.AssignmentId == assignmentId && submission.UserId == userId, cancellationToken);

    public async Task UpsertSubmissionAsync(
        Guid assignmentId, Guid userId, string content, DateTimeOffset submittedAtUtc, CancellationToken cancellationToken)
    {
        var existing = await dbContext.AssignmentSubmissions.FirstOrDefaultAsync(
            submission => submission.AssignmentId == assignmentId && submission.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            existing.Content = content;
            existing.SubmittedAtUtc = submittedAtUtc;
        }
        else
        {
            dbContext.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                UserId = userId,
                Content = content,
                SubmittedAtUtc = submittedAtUtc,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task GradeSubmissionAsync(
        Guid assignmentId,
        Guid userId,
        Guid gradedByUserId,
        decimal score,
        string? feedback,
        DateTimeOffset gradedAtUtc,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext.AssignmentSubmissions.FirstOrDefaultAsync(
            s => s.AssignmentId == assignmentId && s.UserId == userId, cancellationToken);

        if (submission is null)
        {
            return;
        }

        submission.Score = score;
        submission.Feedback = feedback;
        submission.GradedByUserId = gradedByUserId;
        submission.GradedAtUtc = gradedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LearnerAssignmentDto>> GetForLearnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var offerings = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new { enrollment.CourseOfferingId, enrollment.ProgramOfferingId })
            .ToListAsync(cancellationToken);

        var courseOfferingIds = offerings.Where(o => o.CourseOfferingId.HasValue).Select(o => o.CourseOfferingId!.Value).ToList();
        var programOfferingIds = offerings.Where(o => o.ProgramOfferingId.HasValue).Select(o => o.ProgramOfferingId!.Value).ToList();

        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Where(a =>
                (a.CourseOfferingId != null && courseOfferingIds.Contains(a.CourseOfferingId.Value)) ||
                (a.ProgramOfferingId != null && programOfferingIds.Contains(a.ProgramOfferingId.Value)))
            .Select(assignment => new
            {
                assignment.Id,
                assignment.Title,
                assignment.Description,
                assignment.DueAtUtc,
                assignment.MaxScore,
                CourseTitle = assignment.CourseOffering != null ? assignment.CourseOffering.Course!.Title : null,
                ProgramTitle = assignment.ProgramOffering != null ? assignment.ProgramOffering.Program!.Title : null,
            })
            .OrderBy(assignment => assignment.DueAtUtc)
            .ToListAsync(cancellationToken);

        var submissions = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Where(submission => submission.UserId == userId)
            .ToDictionaryAsync(submission => submission.AssignmentId, cancellationToken);

        return assignments
            .Select(assignment =>
            {
                submissions.TryGetValue(assignment.Id, out var submission);
                return new LearnerAssignmentDto(
                    assignment.Id, assignment.Title, assignment.Description,
                    assignment.CourseTitle, assignment.ProgramTitle,
                    assignment.DueAtUtc, assignment.MaxScore,
                    submission?.Content, submission?.SubmittedAtUtc, submission?.Score, submission?.Feedback);
            })
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private static readonly Expression<Func<Assignment, AssignmentDto>> ToDto = assignment => new AssignmentDto(
        assignment.Id,
        assignment.CourseOfferingId,
        assignment.ProgramOfferingId,
        assignment.Title,
        assignment.Description,
        assignment.DueAtUtc,
        assignment.MaxScore,
        assignment.CreatedAtUtc,
        assignment.UpdatedAtUtc);
}
