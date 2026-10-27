using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assignments/{id:guid}")]
[Authorize]
public sealed class AssignmentsController(
    AssignmentService assignmentService,
    IValidator<UpdateAssignmentRequest> updateValidator,
    IValidator<GradeSubmissionRequest> gradeValidator,
    IValidator<SubmitAssignmentRequest> submitValidator,
    IValidator<SubmitAssignmentLinkRequest> submitLinkValidator) : ControllerBase
{
    [HttpPut]
    [Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDto>> Update(
        Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await assignmentService.UpdateAsync(id, GetUserId(), IsPrivileged(), request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpGet("submissions")]
    [Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<SubmissionRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SubmissionRecordDto>>> GetSubmissions(
        Guid id, CancellationToken cancellationToken)
    {
        var outcome = await assignmentService.GetSubmissionsAsync(id, GetUserId(), IsPrivileged(), cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPut("submissions/{userId:guid}/grade")]
    [Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
    [ProducesResponseType<SubmissionRecordDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionRecordDto>> Grade(
        Guid id, Guid userId, GradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        await gradeValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await assignmentService.GradeSubmissionAsync(
            id, userId, GetUserId(), IsPrivileged(), request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPost("submit")]
    [Authorize(Policy = PolicyNames.RequireLearner)]
    [ProducesResponseType<SubmissionConfirmationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionConfirmationDto>> Submit(
        Guid id, SubmitAssignmentRequest request, CancellationToken cancellationToken)
    {
        await submitValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await assignmentService.SubmitAsync(id, GetUserId(), request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPost("submit-link")]
    [Authorize(Policy = PolicyNames.RequireLearner)]
    [ProducesResponseType<SubmissionConfirmationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionConfirmationDto>> SubmitLink(
        Guid id, SubmitAssignmentLinkRequest request, CancellationToken cancellationToken)
    {
        await submitLinkValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await assignmentService.SubmitLinkAsync(id, GetUserId(), request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPost("submit-file")]
    [Authorize(Policy = PolicyNames.RequireLearner)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    [ProducesResponseType<SubmissionConfirmationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionConfirmationDto>> SubmitFile(
        Guid id, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var outcome = await assignmentService.SubmitFileAsync(
            id, GetUserId(), content, file.FileName, file.Length, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpGet("submission/file")]
    [Authorize(Policy = PolicyNames.RequireLearner)]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadMyFile(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await assignmentService.GetLearnerFileAsync(id, GetUserId(), cancellationToken);
        return outcome.Succeeded
            ? File(outcome.Value!.Content, outcome.Value.ContentType, outcome.Value.OriginalFileName)
            : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpGet("submissions/{userId:guid}/file")]
    [Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadLearnerFile(
        Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var outcome = await assignmentService.GetStaffFileAsync(
            id, userId, GetUserId(), IsPrivileged(), cancellationToken);
        return outcome.Succeeded
            ? File(outcome.Value!.Content, outcome.Value.ContentType, outcome.Value.OriginalFileName)
            : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActionResult ToFailureResult(SchedulingFailureKind? kind, string? error)
        => kind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [error ?? "The assignment request is invalid."],
            })),
        };
}
