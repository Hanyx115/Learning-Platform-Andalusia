using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Application.Services;
using AndalusiaAcademy.Domain.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/faqs")]
[AllowAnonymous]
public sealed class FaqsController(CmsService cmsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CmsContentItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CmsContentItemDto>>> Get(CancellationToken cancellationToken)
        => Ok(await cmsService.GetPublishedAsync(CmsContentTypes.Faq, cancellationToken));
}
