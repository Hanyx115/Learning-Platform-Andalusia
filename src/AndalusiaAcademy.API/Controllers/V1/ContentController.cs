using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/content")]
[AllowAnonymous]
public sealed class ContentController(CmsService cmsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CmsContentItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CmsContentItemDto>>> GetPublished(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
        => Ok(await cmsService.GetPublishedAsync(type, cancellationToken));

    [HttpGet("pages/{key}")]
    [ProducesResponseType<CmsContentItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CmsContentItemDto>> GetPage(string key, CancellationToken cancellationToken)
    {
        var page = await cmsService.GetPublishedPageAsync(key, cancellationToken);
        return page is null ? NotFound(new ProblemDetails { Title = "Content page not found." }) : Ok(page);
    }
}
