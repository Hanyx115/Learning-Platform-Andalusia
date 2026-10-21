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
[Route("api/v{version:apiVersion}/course-offerings/{courseOfferingId:guid}/assignments")]
[Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
public sealed class CourseOfferingAssignmentsController(
    AssignmentService assignmentService,
    IValidator<CreateAssignmentRequest> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AssignmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> GetForOffering(
        Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var outcome = await assignmentService.GetForOfferingAsync(
            courseOfferingId, null, GetUserId(), IsPrivileged(), cancellationToken);
        return ToResult(outcome);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDto>> Create(
        Guid courseOfferingId, CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await assignmentService.CreateAsync(courseOfferingId, null, GetUserId(), request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Created($"/api/v1/assignments/{outcome.Value!.Id}", outcome.Value);
        }

        return ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActionResult<IReadOnlyList<AssignmentDto>> ToResult(SchedulingMutationOutcome<IReadOnlyList<AssignmentDto>> outcome)
        => outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);

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
