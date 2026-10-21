using AndalusiaAcademy.Application.DTOs;
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
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProgramsController(
    ProgramService programService,
    OfferingService offeringService,
    IValidator<CreateProgramRequest> createValidator,
    IValidator<UpdateProgramRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginatedResult<ProgramSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<ProgramSummaryDto>>> GetPrograms(
        [FromQuery] CatalogListQuery query,
        CancellationToken cancellationToken)
    {
        var programs = await programService.GetProgramsAsync(query, cancellationToken);
        return Ok(programs);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetProgramBySlug(string slug, CancellationToken cancellationToken)
    {
        var program = await programService.GetProgramBySlugAsync(slug, cancellationToken);
        return program is null ? NotFound() : Ok(program);
    }

    [HttpGet("{slug}/offerings")]
    [ProducesResponseType<IReadOnlyList<OfferingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OfferingDto>>> GetOpenOfferings(
        string slug,
        CancellationToken cancellationToken)
    {
        var offerings = await offeringService.GetOpenProgramOfferingsAsync(slug, cancellationToken);
        return Ok(offerings);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<ProgramSummaryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramSummaryDto>> Create(
        CreateProgramRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await programService.CreateAsync(request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Created($"/api/v1/programs/{outcome.Value!.Slug}", outcome.Value);
        }

        return ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<ProgramSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramSummaryDto>> Update(
        Guid id, UpdateProgramRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await programService.UpdateAsync(id, request, cancellationToken);

        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    private ActionResult ToFailureResult(SchedulingFailureKind? kind, string? error)
        => kind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [error ?? "The program request is invalid."],
            })),
        };
}
