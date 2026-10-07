using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/career-paths")]
public sealed class CareerPathsController(CareerPathService careerPathService) : ControllerBase
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

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCareerPathBySlug(string slug, CancellationToken cancellationToken)
    {
        var careerPath = await careerPathService.GetCareerPathBySlugAsync(slug, cancellationToken);
        return careerPath is null ? NotFound() : Ok(careerPath);
    }
}
