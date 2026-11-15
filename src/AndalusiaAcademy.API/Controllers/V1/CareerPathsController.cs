using AndalusiaAcademy.Application.DTOs;
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
[Route("api/v{version:apiVersion}/career-paths")]
public sealed class CareerPathsController(
    CareerPathService careerPathService,
    IValidator<CareerPathRequest> requestValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginatedResult<CareerPathSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<CareerPathSummaryDto>>> GetCareerPaths(
        [FromQuery] CatalogListQuery query,
        CancellationToken cancellationToken)
    {
        var careerPaths = await careerPathService.GetCareerPathsAsync(query, cancellationToken);
        return Ok(careerPaths);
    }

    [HttpGet("manage")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    public async Task<ActionResult<PaginatedResult<CareerPathSummaryDto>>> GetCareerPathsForManagement(
        [FromQuery] CatalogListQuery query,
        CancellationToken cancellationToken)
        => Ok(await careerPathService.GetCareerPathsForManagementAsync(query, cancellationToken));

    [HttpGet("manage/{id:guid}")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    public async Task<ActionResult<CareerPathDetailDto>> GetCareerPathForManagement(
        Guid id,
        CancellationToken cancellationToken)
    {
        var careerPath = await careerPathService.GetCareerPathByIdAsync(id, cancellationToken);
        return careerPath is null ? NotFound() : Ok(careerPath);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCareerPathBySlug(string slug, CancellationToken cancellationToken)
    {
        var careerPath = await careerPathService.GetCareerPathBySlugAsync(slug, cancellationToken);
        return careerPath is null ? NotFound() : Ok(careerPath);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    public async Task<ActionResult<CareerPathDetailDto>> Create(
        CareerPathRequest request,
        CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await careerPathService.CreateAsync(request, cancellationToken);
        return outcome.Succeeded
            ? Created($"/api/v1/career-paths/{outcome.Value!.Slug}", outcome.Value)
            : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    public async Task<ActionResult<CareerPathDetailDto>> Update(
        Guid id,
        CareerPathRequest request,
        CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await careerPathService.UpdateAsync(id, request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await careerPathService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    private ActionResult ToFailureResult(SchedulingFailureKind? kind, string? error)
        => kind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [error ?? "The career path request is invalid."],
            })),
        };
}
