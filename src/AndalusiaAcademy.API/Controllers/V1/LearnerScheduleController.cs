using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/learner")]
[Authorize(Policy = PolicyNames.RequireLearner)]
public sealed class LearnerScheduleController(SchedulingService schedulingService) : ControllerBase
{
    [HttpGet("upcoming-sessions")]
    [ProducesResponseType<IReadOnlyList<UpcomingSessionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UpcomingSessionDto>>> GetUpcomingSessions(
        CancellationToken cancellationToken)
    {
        var sessions = await schedulingService.GetUpcomingSessionsForLearnerAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);

        return Ok(sessions);
    }
}
