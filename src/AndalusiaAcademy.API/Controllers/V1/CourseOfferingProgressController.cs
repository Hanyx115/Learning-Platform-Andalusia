using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Progress;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/course-offerings/{courseOfferingId:guid}/progress")]
[Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
public sealed class CourseOfferingProgressController(ProgressService progressService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OfferingProgressRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OfferingProgressRecordDto>>> GetForOffering(
        Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var outcome = await progressService.GetForOfferingAsync(
            courseOfferingId, null, GetUserId(), IsPrivileged(), cancellationToken);

        return outcome.Succeeded
            ? Ok(outcome.Value)
            : NotFound(new ProblemDetails { Title = outcome.Error });
    }

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
