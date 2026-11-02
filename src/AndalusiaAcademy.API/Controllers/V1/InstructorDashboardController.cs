using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Instructor;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/instructor")]
[Authorize(Policy = PolicyNames.RequireInstructor)]
public sealed class InstructorDashboardController(InstructorDirectoryService instructorDirectoryService) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType<InstructorOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InstructorOverviewDto>> GetMyOverview(CancellationToken cancellationToken)
        => Ok(await instructorDirectoryService.GetMyOverviewAsync(GetUserId(), cancellationToken));

    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<InstructorSessionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstructorSessionDto>>> GetMySessions(CancellationToken cancellationToken)
        => Ok(await instructorDirectoryService.GetMySessionsAsync(GetUserId(), cancellationToken));

    [HttpGet("offerings")]
    [ProducesResponseType<IReadOnlyList<InstructorOfferingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstructorOfferingDto>>> GetMyOfferings(CancellationToken cancellationToken)
        => Ok(await instructorDirectoryService.GetMyOfferingsAsync(GetUserId(), cancellationToken));

    [HttpGet("students")]
    [ProducesResponseType<IReadOnlyList<InstructorStudentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstructorStudentDto>>> GetMyStudents(CancellationToken cancellationToken)
        => Ok(await instructorDirectoryService.GetMyStudentsAsync(GetUserId(), cancellationToken));

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
