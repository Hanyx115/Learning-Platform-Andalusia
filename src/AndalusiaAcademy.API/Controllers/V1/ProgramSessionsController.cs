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
[Route("api/v{version:apiVersion}/program-sessions")]
[Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
public sealed class ProgramSessionsController(
    SchedulingService schedulingService,
    IValidator<RescheduleCourseSessionRequest> rescheduleValidator,
    IValidator<CancelCourseSessionRequest> cancelValidator) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProgramSessionDto>> Reschedule(
        Guid id, RescheduleCourseSessionRequest request, CancellationToken cancellationToken)
    {
        await rescheduleValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.RescheduleProgramSessionAsync(id, request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : Failure(outcome);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ProgramSessionDto>> Cancel(
        Guid id, CancelCourseSessionRequest request, CancellationToken cancellationToken)
    {
        await cancelValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.CancelProgramSessionAsync(id, request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : Failure(outcome);
    }

    private ActionResult<ProgramSessionDto> Failure(SchedulingMutationOutcome<ProgramSessionDto> outcome)
        => outcome.FailureKind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = outcome.Error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = outcome.Error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [outcome.Error ?? "The session request is invalid."],
            })),
        };
}
