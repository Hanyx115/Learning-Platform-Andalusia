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
    public async Task<IActionResult> GetPrograms(CancellationToken cancellationToken)
    {
        var programs = await programService.GetProgramsAsync(cancellationToken);
        return Ok(programs);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetProgramBySlug(string slug, CancellationToken cancellationToken)
    {
        var program = await programService.GetProgramBySlugAsync(slug, cancellationToken);
        return program is null ? NotFound() : Ok(program);
    }
}
