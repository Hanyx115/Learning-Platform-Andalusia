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
[Route("api/v{version:apiVersion}/program-offerings/{programOfferingId:guid}/sessions")]
public sealed class ProgramOfferingSessionsController(
    SchedulingService schedulingService,
    IValidator<ScheduleCourseSessionRequest> validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProgramSessionDto>>> GetPublic(
        Guid programOfferingId, CancellationToken cancellationToken)
        => Ok(await schedulingService.GetSessionsForProgramOfferingAsync(
            programOfferingId, includeCancelled: true, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    public async Task<ActionResult<ProgramSessionDto>> Create(
        Guid programOfferingId, ScheduleCourseSessionRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.ScheduleProgramSessionAsync(programOfferingId, request, cancellationToken);
        return outcome.Succeeded
            ? Created($"/api/v1/program-sessions/{outcome.Value!.Id}", outcome.Value)
            : Failure(outcome);
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
