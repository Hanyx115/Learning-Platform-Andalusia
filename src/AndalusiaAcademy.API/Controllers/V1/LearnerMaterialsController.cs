using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/learner/enrollments/{enrollmentId:guid}/materials")]
[Authorize(Policy = PolicyNames.RequireLearner)]
public sealed class LearnerMaterialsController(CourseMaterialService materialService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseMaterialDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CourseMaterialDto>>> Get(
        Guid enrollmentId,
        CancellationToken cancellationToken)
    {
        var materials = await materialService.GetForLearnerEnrollmentAsync(
            enrollmentId,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);

        return materials is null
            ? NotFound(new ProblemDetails { Title = "Enrollment not found or learning access is unavailable." })
            : Ok(materials);
    }

    [HttpGet("{materialId:guid}/file")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFile(
        Guid enrollmentId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var outcome = await materialService.GetLearnerFileAsync(
            enrollmentId,
            materialId,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);
        return outcome.Succeeded
            ? File(outcome.Value!.Content, outcome.Value.ContentType, outcome.Value.OriginalFileName)
            : NotFound(new ProblemDetails { Title = outcome.Error });
    }
}
