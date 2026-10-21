using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class AssignmentService(IAssignmentRepository repository, TimeProvider timeProvider)
{
    public async Task<SchedulingMutationOutcome<AssignmentDto>> CreateAsync(
        Guid? courseOfferingId,
        Guid? programOfferingId,
        Guid createdByUserId,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await repository.OfferingExistsAsync(courseOfferingId, programOfferingId, cancellationToken))
        {
            return Fail<AssignmentDto>(SchedulingFailureKind.NotFound, "Offering not found.");
        }

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseOfferingId = courseOfferingId,
            ProgramOfferingId = programOfferingId,
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            DueAtUtc = request.DueAtUtc.ToUniversalTime(),
            MaxScore = request.MaxScore,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        repository.Add(assignment);
        await repository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<AssignmentDto>.Success(
            (await repository.GetByIdAsync(assignment.Id, cancellationToken))!);
    }

    public async Task<SchedulingMutationOutcome<AssignmentDto>> UpdateAsync(
        Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(id, cancellationToken);
        if (assignment is null)
        {
            return Fail<AssignmentDto>(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        assignment.Title = request.Title.Trim();
        assignment.Description = NormalizeOptional(request.Description);
        assignment.DueAtUtc = request.DueAtUtc.ToUniversalTime();
        assignment.MaxScore = request.MaxScore;
        assignment.UpdatedAtUtc = timeProvider.GetUtcNow();

        await repository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<AssignmentDto>.Success(
            (await repository.GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<SchedulingMutationOutcome<IReadOnlyList<AssignmentDto>>> GetForOfferingAsync(
        Guid? courseOfferingId,
        Guid? programOfferingId,
        Guid callerUserId,
        bool callerIsPrivileged,
        CancellationToken cancellationToken)
    {
        if (!callerIsPrivileged &&
            !await repository.IsInstructorForOfferingAsync(callerUserId, courseOfferingId, programOfferingId, cancellationToken))
        {
            return Fail<IReadOnlyList<AssignmentDto>>(SchedulingFailureKind.NotFound, "Offering not found.");
        }

        var assignments = await repository.GetForOfferingAsync(courseOfferingId, programOfferingId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<AssignmentDto>>.Success(assignments);
    }

    public async Task<SchedulingMutationOutcome<IReadOnlyList<SubmissionRecordDto>>> GetSubmissionsAsync(
        Guid assignmentId, Guid callerUserId, bool callerIsPrivileged, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(assignmentId, cancellationToken);
        if (assignment is null || (!callerIsPrivileged && !await repository.IsInstructorForOfferingAsync(
            callerUserId, assignment.CourseOfferingId, assignment.ProgramOfferingId, cancellationToken)))
        {
            return Fail<IReadOnlyList<SubmissionRecordDto>>(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        var roster = await repository.GetRosterAsync(assignmentId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<SubmissionRecordDto>>.Success(roster);
    }

    public async Task<SchedulingMutationOutcome<SubmissionRecordDto>> GradeSubmissionAsync(
        Guid assignmentId,
        Guid learnerUserId,
        Guid callerUserId,
        bool callerIsPrivileged,
        GradeSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(assignmentId, cancellationToken);
        if (assignment is null || (!callerIsPrivileged && !await repository.IsInstructorForOfferingAsync(
            callerUserId, assignment.CourseOfferingId, assignment.ProgramOfferingId, cancellationToken)))
        {
            return Fail<SubmissionRecordDto>(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        if (request.Score > assignment.MaxScore)
        {
            return Fail<SubmissionRecordDto>(
                SchedulingFailureKind.Validation, $"Score cannot exceed the maximum of {assignment.MaxScore}.");
        }

        var submission = await repository.GetSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        if (submission is null)
        {
            return Fail<SubmissionRecordDto>(SchedulingFailureKind.Conflict, "This learner has not submitted this assignment yet.");
        }

        await repository.GradeSubmissionAsync(
            assignmentId, learnerUserId, callerUserId, request.Score, NormalizeOptional(request.Feedback),
            timeProvider.GetUtcNow(), cancellationToken);

        var roster = await repository.GetRosterAsync(assignmentId, cancellationToken);
        var updated = roster.Single(record => record.UserId == learnerUserId);
        return SchedulingMutationOutcome<SubmissionRecordDto>.Success(updated);
    }

    public async Task<SchedulingMutationOutcome<SubmissionConfirmationDto>> SubmitAsync(
        Guid assignmentId, Guid learnerUserId, SubmitAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            return Fail<SubmissionConfirmationDto>(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        if (!await repository.IsEnrolledAsync(
            learnerUserId, assignment.CourseOfferingId, assignment.ProgramOfferingId, cancellationToken))
        {
            return Fail<SubmissionConfirmationDto>(
                SchedulingFailureKind.Validation, "You are not enrolled in this offering.");
        }

        var existing = await repository.GetSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        if (existing?.Score is not null)
        {
            return Fail<SubmissionConfirmationDto>(
                SchedulingFailureKind.Conflict, "This assignment has already been graded and can no longer be resubmitted.");
        }

        var submittedAtUtc = timeProvider.GetUtcNow();
        await repository.UpsertSubmissionAsync(assignmentId, learnerUserId, request.Content.Trim(), submittedAtUtc, cancellationToken);

        return SchedulingMutationOutcome<SubmissionConfirmationDto>.Success(
            new SubmissionConfirmationDto(assignmentId, request.Content.Trim(), submittedAtUtc));
    }

    public Task<IReadOnlyList<LearnerAssignmentDto>> GetMyAssignmentsAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetForLearnerAsync(userId, cancellationToken);

    private static SchedulingMutationOutcome<T> Fail<T>(SchedulingFailureKind kind, string error)
        where T : class
        => SchedulingMutationOutcome<T>.Failure(kind, error);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
