using AndalusiaAcademy.Application.DTOs.Operations;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/operations")]
[Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
public sealed class OperationsController(OperationsDashboardService dashboardService) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType<OperationsOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationsOverviewDto>> GetOverview(CancellationToken cancellationToken)
        => Ok(await dashboardService.GetOverviewAsync(cancellationToken));
}
