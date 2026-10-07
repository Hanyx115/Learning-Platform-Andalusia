using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProgramsController(ProgramService programService) : ControllerBase
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
}
