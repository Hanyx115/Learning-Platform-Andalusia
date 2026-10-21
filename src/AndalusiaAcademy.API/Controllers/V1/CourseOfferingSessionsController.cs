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
[Route("api/v{version:apiVersion}/course-offerings/{courseOfferingId:guid}/sessions")]
public sealed class CourseOfferingSessionsController(
    SchedulingService schedulingService,
    IValidator<ScheduleCourseSessionRequest> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseSessionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseSessionDto>>> GetPublic(
        Guid courseOfferingId,
        CancellationToken cancellationToken)
        => Ok(await schedulingService.GetSessionsForOfferingAsync(
            courseOfferingId, includeCancelled: false, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<CourseSessionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseSessionDto>> Create(
        Guid courseOfferingId,
        ScheduleCourseSessionRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.ScheduleSessionAsync(
            courseOfferingId, request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Created($"/api/v1/course-sessions/{outcome.Value!.Id}", outcome.Value);
        }

        return ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    private ActionResult<CourseSessionDto> ToFailureResult(SchedulingFailureKind? kind, string? error)
        => kind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [error ?? "The session request is invalid."],
            })),
        };
}
