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
    public async Task<IActionResult> GetCareerPaths(CancellationToken cancellationToken)
    {
        var careerPaths = await careerPathService.GetCareerPathsAsync(cancellationToken);
        return Ok(careerPaths);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCareerPathBySlug(string slug, CancellationToken cancellationToken)
    {
        var careerPath = await careerPathService.GetCareerPathBySlugAsync(slug, cancellationToken);
        return careerPath is null ? NotFound() : Ok(careerPath);
    }
}
