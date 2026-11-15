using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Skills;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/skills/passport")]
[Authorize(Policy = PolicyNames.RequireLearner)]
public sealed class SkillsPassportController(SkillsPassportService service) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<SkillsPassportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SkillsPassportDto>> GetMine(CancellationToken cancellationToken)
        => Ok(await service.GetMineAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken));
}
