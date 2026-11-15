using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Attendance;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/program-sessions/{programSessionId:guid}/attendance")]
[Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
public sealed class ProgramSessionAttendanceController(
    AttendanceService attendanceService,
    IValidator<MarkSessionAttendanceRequest> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SessionAttendanceRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SessionAttendanceRecordDto>>> GetRoster(
        Guid programSessionId,
        CancellationToken cancellationToken)
    {
        var outcome = await attendanceService.GetProgramSessionRosterAsync(
            programSessionId, GetUserId(), IsPrivileged(), cancellationToken);
        return ToResult(outcome);
    }

    [HttpPut]
    [ProducesResponseType<IReadOnlyList<SessionAttendanceRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SessionAttendanceRecordDto>>> Mark(
        Guid programSessionId,
        MarkSessionAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await attendanceService.MarkProgramSessionAttendanceAsync(
            programSessionId, GetUserId(), IsPrivileged(), request, cancellationToken);
        return ToResult(outcome);
    }

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActionResult<IReadOnlyList<SessionAttendanceRecordDto>> ToResult(
        SchedulingMutationOutcome<IReadOnlyList<SessionAttendanceRecordDto>> outcome)
    {
        if (outcome.Succeeded)
        {
            return Ok(outcome.Value);
        }

        return outcome.FailureKind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = outcome.Error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = outcome.Error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [outcome.Error ?? "The attendance request is invalid."],
            })),
        };
    }
}
