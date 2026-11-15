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
[Route("api/v{version:apiVersion}/training-locations")]
public sealed class TrainingLocationsController(
    SchedulingService schedulingService,
    IValidator<CreateTrainingLocationRequest> createValidator,
    IValidator<UpdateTrainingLocationRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TrainingLocationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrainingLocationDto>>> GetActive(
        CancellationToken cancellationToken)
        => Ok(await schedulingService.GetLocationsAsync(includeInactive: false, cancellationToken));

    [HttpGet("manage")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<TrainingLocationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrainingLocationDto>>> GetForManagement(
        CancellationToken cancellationToken)
        => Ok(await schedulingService.GetLocationsAsync(includeInactive: true, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<TrainingLocationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainingLocationDto>> Create(
        CreateTrainingLocationRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.CreateLocationAsync(request, cancellationToken);

        return outcome.Succeeded
            ? Created($"/api/v1/training-locations/{outcome.Value!.Id}", outcome.Value)
            : Conflict(new ProblemDetails { Title = outcome.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<TrainingLocationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainingLocationDto>> Update(
        Guid id,
        UpdateTrainingLocationRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await schedulingService.UpdateLocationAsync(id, request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Ok(outcome.Value);
        }

        return outcome.FailureKind == SchedulingFailureKind.NotFound
            ? NotFound(new ProblemDetails { Title = outcome.Error })
            : Conflict(new ProblemDetails { Title = outcome.Error });
    }
}
