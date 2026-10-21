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
[Route("api/v{version:apiVersion}/course-sessions")]
[Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
public sealed class CourseSessionsController(
    SchedulingService schedulingService,
    IValidator<RescheduleCourseSessionRequest> rescheduleValidator,
    IValidator<CancelCourseSessionRequest> cancelValidator) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CourseSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseSessionDto>> Reschedule(
        Guid id,
        RescheduleCourseSessionRequest request,
        CancellationToken cancellationToken)
    {
        await rescheduleValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.RescheduleSessionAsync(id, request, cancellationToken);
        return ToResult(outcome);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<CourseSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseSessionDto>> Cancel(
        Guid id,
        CancelCourseSessionRequest request,
        CancellationToken cancellationToken)
    {
        await cancelValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.CancelSessionAsync(id, request, cancellationToken);
        return ToResult(outcome);
    }

    private ActionResult<CourseSessionDto> ToResult(SchedulingMutationOutcome<CourseSessionDto> outcome)
    {
        if (outcome.Succeeded)
        {
            return Ok(outcome.Value);
        }

        return outcome.FailureKind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = outcome.Error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = outcome.Error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [outcome.Error ?? "The session request is invalid."],
            })),
        };
    }
}
