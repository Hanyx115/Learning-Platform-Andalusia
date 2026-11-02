using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Enrollments;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enrollments")]
[Authorize]
public sealed class EnrollmentsController(EnrollmentReadService enrollmentService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<EnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> GetMine(CancellationToken cancellationToken)
    {
        var enrollments = await enrollmentService.GetMyEnrollmentsAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);

        return Ok(enrollments);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<AdminEnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminEnrollmentDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await enrollmentService.GetAllAsync(cancellationToken));

    [HttpGet("manage")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<PaginatedResult<AdminEnrollmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<AdminEnrollmentDto>>> GetPage(
        [FromQuery] EnrollmentDirectoryQuery query,
        CancellationToken cancellationToken)
        => Ok(await enrollmentService.GetPageAsync(query, cancellationToken));
}
