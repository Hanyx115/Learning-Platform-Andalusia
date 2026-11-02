using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Compensation;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/balance")]
[Authorize]
public sealed class BalanceController(BalanceService balanceService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<BalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceDto>> GetMine(CancellationToken cancellationToken)
        => Ok(await balanceService.GetMineAsync(GetUserId(), cancellationToken));

    [HttpPost("me/demo-credit")]
    [ProducesResponseType<BalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BalanceDto>> AddDemoCredit(
        DemoBalanceCreditRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await balanceService.AddDemoCreditAsync(GetUserId(), request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [exception.Message],
            }));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
