using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.Application.Services;

public sealed class AssignmentService(
    IAssignmentRepository repository,
    IAssignmentSubmissionFileStore fileStore,
    INotificationOutbox notificationOutbox,
    TimeProvider timeProvider,
    ILogger<AssignmentService> logger)
{
    public const long MaximumSubmissionFileSizeBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedFileTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".txt"] = "text/plain",
        };

    public async Task<SchedulingMutationOutcome<AssignmentDto>> CreateAsync(
        Guid? courseOfferingId,
        Guid? programOfferingId,
        Guid createdByUserId,
        bool callerIsPrivileged,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await repository.OfferingExistsAsync(courseOfferingId, programOfferingId, cancellationToken))
        {
            return Fail<AssignmentDto>(SchedulingFailureKind.NotFound, "Offering not found.");
        }

        if (!callerIsPrivileged && !await repository.IsInstructorForOfferingAsync(
            createdByUserId, courseOfferingId, programOfferingId, cancellationToken))
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
        Guid id,
        Guid callerUserId,
        bool callerIsPrivileged,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(id, cancellationToken);
        if (assignment is null)
        {
            return Fail<AssignmentDto>(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        if (!callerIsPrivileged && !await repository.IsInstructorForOfferingAsync(
            callerUserId, assignment.CourseOfferingId, assignment.ProgramOfferingId, cancellationToken))
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
        var validation = await ValidateSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        if (!validation.Succeeded)
        {
            return Fail<SubmissionConfirmationDto>(validation.FailureKind!.Value, validation.Error!);
        }

        var content = request.Content.Trim();
        var previousStoredFileName = validation.ExistingSubmission?.StoredFileName;
        var submittedAtUtc = timeProvider.GetUtcNow();
        await repository.UpsertSubmissionAsync(
            assignmentId,
            learnerUserId,
            new AssignmentSubmissionWriteModel(
                content, AssignmentSubmissionTypes.Text, null, null, null, null, null),
            submittedAtUtc,
            cancellationToken);
        await DeletePreviousFileAsync(previousStoredFileName, cancellationToken);
        await QueueSubmissionNotificationAsync(validation.Assignment!, learnerUserId, submittedAtUtc, cancellationToken);

        return SchedulingMutationOutcome<SubmissionConfirmationDto>.Success(
            new SubmissionConfirmationDto(
                assignmentId, content, AssignmentSubmissionTypes.Text, null, null, null, submittedAtUtc));
    }

    public async Task<SchedulingMutationOutcome<SubmissionConfirmationDto>> SubmitLinkAsync(
        Guid assignmentId,
        Guid learnerUserId,
        SubmitAssignmentLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out var link) ||
            (link.Scheme != Uri.UriSchemeHttp && link.Scheme != Uri.UriSchemeHttps))
        {
            return Fail<SubmissionConfirmationDto>(
                SchedulingFailureKind.Validation, "Enter a valid http or https link.");
        }

        var validation = await ValidateSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        if (!validation.Succeeded)
        {
            return Fail<SubmissionConfirmationDto>(validation.FailureKind!.Value, validation.Error!);
        }

        var normalizedLink = link.AbsoluteUri;
        var previousStoredFileName = validation.ExistingSubmission?.StoredFileName;
        var submittedAtUtc = timeProvider.GetUtcNow();
        await repository.UpsertSubmissionAsync(
            assignmentId,
            learnerUserId,
            new AssignmentSubmissionWriteModel(
                normalizedLink, AssignmentSubmissionTypes.Link, normalizedLink, null, null, null, null),
            submittedAtUtc,
            cancellationToken);
        await DeletePreviousFileAsync(previousStoredFileName, cancellationToken);
        await QueueSubmissionNotificationAsync(validation.Assignment!, learnerUserId, submittedAtUtc, cancellationToken);

        return SchedulingMutationOutcome<SubmissionConfirmationDto>.Success(
            new SubmissionConfirmationDto(
                assignmentId, normalizedLink, AssignmentSubmissionTypes.Link, normalizedLink, null, null, submittedAtUtc));
    }

    public async Task<SchedulingMutationOutcome<SubmissionConfirmationDto>> SubmitFileAsync(
        Guid assignmentId,
        Guid learnerUserId,
        Stream content,
        string originalFileName,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        if (fileSizeBytes <= 0)
        {
            return Fail<SubmissionConfirmationDto>(SchedulingFailureKind.Validation, "Choose a non-empty document.");
        }

        if (fileSizeBytes > MaximumSubmissionFileSizeBytes)
        {
            return Fail<SubmissionConfirmationDto>(SchedulingFailureKind.Validation, "The document cannot exceed 10 MB.");
        }

        var safeFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || !AllowedFileTypes.TryGetValue(extension, out var contentType))
        {
            return Fail<SubmissionConfirmationDto>(
                SchedulingFailureKind.Validation,
                "Upload a PDF, Word, PowerPoint, Excel, or text document.");
        }

        var validation = await ValidateSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        if (!validation.Succeeded)
        {
            return Fail<SubmissionConfirmationDto>(validation.FailureKind!.Value, validation.Error!);
        }

        var previousStoredFileName = validation.ExistingSubmission?.StoredFileName;
        var storedFile = await fileStore.StoreAsync(content, safeFileName, contentType, cancellationToken);
        if (storedFile.SizeBytes <= 0 || storedFile.SizeBytes > MaximumSubmissionFileSizeBytes)
        {
            await fileStore.DeleteAsync(storedFile.StoredFileName, CancellationToken.None);
            return Fail<SubmissionConfirmationDto>(
                SchedulingFailureKind.Validation,
                storedFile.SizeBytes <= 0 ? "Choose a non-empty document." : "The document cannot exceed 10 MB.");
        }

        var submittedAtUtc = timeProvider.GetUtcNow();

        try
        {
            await repository.UpsertSubmissionAsync(
                assignmentId,
                learnerUserId,
                new AssignmentSubmissionWriteModel(
                    storedFile.OriginalFileName,
                    AssignmentSubmissionTypes.File,
                    null,
                    storedFile.OriginalFileName,
                    storedFile.StoredFileName,
                    storedFile.ContentType,
                    storedFile.SizeBytes),
                submittedAtUtc,
                cancellationToken);
        }
        catch
        {
            await fileStore.DeleteAsync(storedFile.StoredFileName, CancellationToken.None);
            throw;
        }

        if (!string.Equals(
            previousStoredFileName,
            storedFile.StoredFileName,
            StringComparison.Ordinal))
        {
            await DeletePreviousFileAsync(previousStoredFileName, cancellationToken);
        }

        await QueueSubmissionNotificationAsync(validation.Assignment!, learnerUserId, submittedAtUtc, cancellationToken);

        return SchedulingMutationOutcome<SubmissionConfirmationDto>.Success(
            new SubmissionConfirmationDto(
                assignmentId,
                storedFile.OriginalFileName,
                AssignmentSubmissionTypes.File,
                null,
                storedFile.OriginalFileName,
                storedFile.SizeBytes,
                submittedAtUtc));
    }

    public async Task<SchedulingMutationOutcome<AssignmentSubmissionFileDto>> GetLearnerFileAsync(
        Guid assignmentId,
        Guid learnerUserId,
        CancellationToken cancellationToken)
    {
        var submission = await repository.GetSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        return await OpenSubmissionFileAsync(submission, cancellationToken);
    }

    public async Task<SchedulingMutationOutcome<AssignmentSubmissionFileDto>> GetStaffFileAsync(
        Guid assignmentId,
        Guid learnerUserId,
        Guid callerUserId,
        bool callerIsPrivileged,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(assignmentId, cancellationToken);
        if (assignment is null || (!callerIsPrivileged && !await repository.IsInstructorForOfferingAsync(
            callerUserId, assignment.CourseOfferingId, assignment.ProgramOfferingId, cancellationToken)))
        {
            return Fail<AssignmentSubmissionFileDto>(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        var submission = await repository.GetSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        return await OpenSubmissionFileAsync(submission, cancellationToken);
    }

    public Task<IReadOnlyList<LearnerAssignmentDto>> GetMyAssignmentsAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetForLearnerAsync(userId, cancellationToken);

    private async Task<SubmissionValidation> ValidateSubmissionAsync(
        Guid assignmentId,
        Guid learnerUserId,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForUpdateAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            return SubmissionValidation.Failure(SchedulingFailureKind.NotFound, "Assignment not found.");
        }

        if (!await repository.IsEnrolledAsync(
            learnerUserId, assignment.CourseOfferingId, assignment.ProgramOfferingId, cancellationToken))
        {
            return SubmissionValidation.Failure(
                SchedulingFailureKind.Validation, "You are not enrolled in this offering.");
        }

        var existing = await repository.GetSubmissionAsync(assignmentId, learnerUserId, cancellationToken);
        if (existing?.Score is not null)
        {
            return SubmissionValidation.Failure(
                SchedulingFailureKind.Conflict, "This assignment has already been graded and can no longer be resubmitted.");
        }

        return SubmissionValidation.Success(assignment, existing);
    }

    private async Task<SchedulingMutationOutcome<AssignmentSubmissionFileDto>> OpenSubmissionFileAsync(
        AssignmentSubmission? submission,
        CancellationToken cancellationToken)
    {
        if (submission?.SubmissionType != AssignmentSubmissionTypes.File ||
            string.IsNullOrWhiteSpace(submission.StoredFileName) ||
            string.IsNullOrWhiteSpace(submission.OriginalFileName) ||
            string.IsNullOrWhiteSpace(submission.FileContentType))
        {
            return Fail<AssignmentSubmissionFileDto>(SchedulingFailureKind.NotFound, "Submitted document not found.");
        }

        var stream = await fileStore.OpenReadAsync(submission.StoredFileName, cancellationToken);
        return stream is null
            ? Fail<AssignmentSubmissionFileDto>(SchedulingFailureKind.NotFound, "Submitted document not found.")
            : SchedulingMutationOutcome<AssignmentSubmissionFileDto>.Success(
                new AssignmentSubmissionFileDto(stream, submission.FileContentType, submission.OriginalFileName));
    }

    private Task DeletePreviousFileAsync(string? storedFileName, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(storedFileName)
            ? Task.CompletedTask
            : fileStore.DeleteAsync(storedFileName, cancellationToken);

    private static SchedulingMutationOutcome<T> Fail<T>(SchedulingFailureKind kind, string error)
        where T : class
        => SchedulingMutationOutcome<T>.Failure(kind, error);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task QueueSubmissionNotificationAsync(
        Assignment assignment,
        Guid learnerUserId,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationOutbox.EnqueueAsync(
                new NotificationEventRequest(
                    $"assignment-submitted:{assignment.Id:N}:{learnerUserId:N}:{submittedAtUtc.UtcTicks}",
                    NotificationTemplateCodes.AssignmentSubmitted,
                    [assignment.CreatedByUserId],
                    new Dictionary<string, string>
                    {
                        ["learnerName"] = "Learner",
                        ["assignmentTitle"] = assignment.Title,
                    },
                    $"/instructor/assignments/{assignment.Id}/submissions"),
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Could not queue assignment notification for {AssignmentId}.", assignment.Id);
        }
    }

    private sealed record SubmissionValidation(
        bool Succeeded,
        Assignment? Assignment,
        AssignmentSubmission? ExistingSubmission,
        SchedulingFailureKind? FailureKind,
        string? Error)
    {
        public static SubmissionValidation Success(Assignment assignment, AssignmentSubmission? existingSubmission)
            => new(true, assignment, existingSubmission, null, null);

        public static SubmissionValidation Failure(SchedulingFailureKind kind, string error)
            => new(false, null, null, kind, error);
    }
}
