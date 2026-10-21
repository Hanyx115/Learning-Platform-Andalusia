using AndalusiaAcademy.Application.DTOs.Applications;
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
public sealed class ScheduledOfferingsController(
    SchedulingService schedulingService,
    IValidator<ManageOfferingRequest> validator) : ControllerBase
{
    [HttpGet("api/v{version:apiVersion}/course-offerings/{id:guid}")]
    public async Task<ActionResult<OfferingDto>> GetCourseOffering(Guid id, CancellationToken cancellationToken)
    {
        var offering = await schedulingService.GetCourseOfferingDetailsAsync(id, cancellationToken);
        return offering is null ? NotFound() : Ok(offering);
    }

    [HttpGet("api/v{version:apiVersion}/program-offerings/{id:guid}")]
    public async Task<ActionResult<OfferingDto>> GetProgramOffering(Guid id, CancellationToken cancellationToken)
    {
        var offering = await schedulingService.GetProgramOfferingDetailsAsync(id, cancellationToken);
        return offering is null ? NotFound() : Ok(offering);
    }

    [HttpPost("api/v{version:apiVersion}/courses/{courseId:guid}/offerings")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    public async Task<ActionResult<OfferingDto>> CreateCourseOffering(
        Guid courseId, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.CreateCourseOfferingAsync(courseId, request, cancellationToken);
        return outcome.Succeeded
            ? Created($"/api/v1/course-offerings/{outcome.Value!.Id}", outcome.Value)
            : Failure(outcome);
    }

    [HttpPut("api/v{version:apiVersion}/course-offerings/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    public async Task<ActionResult<OfferingDto>> UpdateCourseOffering(
        Guid id, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.UpdateCourseOfferingAsync(id, request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : Failure(outcome);
    }

    [HttpPost("api/v{version:apiVersion}/programs/{programId:guid}/offerings")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    public async Task<ActionResult<OfferingDto>> CreateProgramOffering(
        Guid programId, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.CreateProgramOfferingAsync(programId, request, cancellationToken);
        return outcome.Succeeded
            ? Created($"/api/v1/program-offerings/{outcome.Value!.Id}", outcome.Value)
            : Failure(outcome);
    }

    [HttpPut("api/v{version:apiVersion}/program-offerings/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    public async Task<ActionResult<OfferingDto>> UpdateProgramOffering(
        Guid id, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.UpdateProgramOfferingAsync(id, request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : Failure(outcome);
    }

    private ActionResult<OfferingDto> Failure(SchedulingMutationOutcome<OfferingDto> outcome)
        => outcome.FailureKind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = outcome.Error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = outcome.Error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [outcome.Error ?? "The offering request is invalid."],
            })),
        };
}
