using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Admin;
using AndalusiaAcademy.Application.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminUserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await adminUserService.GetAllAsync(cancellationToken));

    [HttpPut("{userId:guid}/status")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDto>> SetStatus(
        Guid userId,
        UpdateAdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await adminUserService.SetActiveAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            userId,
            request.IsActive,
            cancellationToken);

        return ToActionResult(outcome);
    }

    [HttpPut("{userId:guid}/roles")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateRoles(
        Guid userId,
        UpdateAdminUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Roles is null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Roles)] = ["Roles are required."],
            }));
        }

        var outcome = await adminUserService.UpdateRolesAsync(userId, request.Roles, cancellationToken);
        return ToActionResult(outcome);
    }

    private ActionResult<AdminUserDto> ToActionResult(AdminUserUpdateOutcome outcome)
    {
        if (outcome.Succeeded)
        {
            return Ok(outcome.User);
        }

        return outcome.Failure switch
        {
            AdminUserUpdateFailure.NotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = outcome.Error,
            }),
            AdminUserUpdateFailure.Conflict => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = outcome.Error,
            }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [outcome.Error ?? "The user could not be updated."],
            })),
        };
    }
}
