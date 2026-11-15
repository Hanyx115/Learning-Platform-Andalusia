using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.DTOs.Attendance;
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
[Route("api/v{version:apiVersion}/learner")]
[Authorize(Policy = PolicyNames.RequireLearner)]
public sealed class LearnerScheduleController(
    SchedulingService schedulingService,
    AttendanceService attendanceService,
    AssignmentService assignmentService,
    ProgressService progressService) : ControllerBase
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

    [HttpGet("attendance")]
    [ProducesResponseType<IReadOnlyList<LearnerSessionAttendanceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LearnerSessionAttendanceDto>>> GetMyAttendance(
        CancellationToken cancellationToken)
    {
        var records = await attendanceService.GetMyAttendanceAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);

        return Ok(records);
    }

    [HttpGet("assignments")]
    [ProducesResponseType<IReadOnlyList<LearnerAssignmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LearnerAssignmentDto>>> GetMyAssignments(
        CancellationToken cancellationToken)
    {
        var assignments = await assignmentService.GetMyAssignmentsAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);

        return Ok(assignments);
    }

    [HttpGet("progress")]
    [ProducesResponseType<IReadOnlyList<LearnerProgressDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LearnerProgressDto>>> GetMyProgress(
        CancellationToken cancellationToken)
    {
        var progress = await progressService.GetMyProgressAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);

        return Ok(progress);
    }
}
