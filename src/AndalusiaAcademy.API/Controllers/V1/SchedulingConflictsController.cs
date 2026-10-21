using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scheduling-conflicts")]
[Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
public sealed class SchedulingConflictsController(SchedulingService schedulingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchedulingConflictDto>>> Get(
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? locationId,
        Guid? instructorUserId,
        CancellationToken cancellationToken)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            ModelState.AddModelError(nameof(endsAtUtc), "The end time must be after the start time.");
            return ValidationProblem(ModelState);
        }
        if (!locationId.HasValue && !instructorUserId.HasValue)
        {
            ModelState.AddModelError("filter", "Provide a location or instructor filter.");
            return ValidationProblem(ModelState);
        }

        return Ok(await schedulingService.GetConflictsAsync(
            startsAtUtc, endsAtUtc, locationId, instructorUserId, cancellationToken));
    }
}
