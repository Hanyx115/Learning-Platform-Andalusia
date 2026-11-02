using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Compensation;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compensation")]
[Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
public sealed class CompensationController(CompensationService compensationService) : ControllerBase
{
    [HttpGet("instructor-targets")]
    public async Task<ActionResult<IReadOnlyList<InstructorCompensationTargetDto>>> GetInstructorTargets(
        CancellationToken cancellationToken)
        => Ok(await compensationService.GetInstructorTargetsAsync(cancellationToken));

    [HttpPut("instructor-rates")]
    [Authorize(Policy = PolicyNames.RequireManager)]
    public async Task<IActionResult> SetInstructorRate(
        SetInstructorCourseRateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await compensationService.SetInstructorRateAsync(request, GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = exception.Message });
        }
    }

    [HttpGet("staff-rates")]
    public async Task<ActionResult<IReadOnlyList<StaffCompensationRateDto>>> GetStaffRates(CancellationToken cancellationToken)
        => Ok(await compensationService.GetStaffRatesAsync(cancellationToken));

    [HttpPut("staff-rates/{userId:guid}")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> SetStaffRate(
        Guid userId,
        SetStaffMonthlyRateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await compensationService.SetStaffRateAsync(userId, request, GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = exception.Message });
        }
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<IReadOnlyList<CompensationPayoutDto>>> GetPayouts(CancellationToken cancellationToken)
        => Ok(await compensationService.GetPayoutsAsync(cancellationToken));

    [HttpPost("instructor-rates/{rateId:guid}/payout")]
    [Authorize(Policy = PolicyNames.RequireManager)]
    public async Task<ActionResult<CompensationPayoutDto>> CreateInstructorPayout(
        Guid rateId,
        CancellationToken cancellationToken)
        => ToActionResult(await compensationService.CreateInstructorPayoutAsync(rateId, cancellationToken));

    [HttpPost("staff-rates/{userId:guid}/payout")]
    [Authorize(Policy = PolicyNames.RequireManager)]
    public async Task<ActionResult<CompensationPayoutDto>> CreateStaffPayout(
        Guid userId,
        CreateStaffMonthlyPayoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return ToActionResult(await compensationService.CreateStaffPayoutAsync(userId, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
    }

    [HttpPost("payouts/{payoutId:guid}/review")]
    [Authorize(Policy = PolicyNames.RequireManager)]
    public async Task<ActionResult<CompensationPayoutDto>> ReviewPayout(
        Guid payoutId,
        CancellationToken cancellationToken)
        => ToActionResult(await compensationService.ReviewPayoutAsync(payoutId, GetUserId(), cancellationToken));

    [HttpPost("payouts/{payoutId:guid}/approve")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<ActionResult<CompensationPayoutDto>> ApprovePayout(
        Guid payoutId,
        CancellationToken cancellationToken)
        => ToActionResult(await compensationService.ApprovePayoutAsync(payoutId, GetUserId(), cancellationToken));

    private ActionResult<CompensationPayoutDto> ToActionResult(CompensationActionOutcome outcome)
        => outcome.Succeeded
            ? Ok(outcome.Payout)
            : Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = outcome.Error });

    private ActionResult Invalid(string message)
        => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["request"] = [message],
        }));

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
