using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/course-materials")]
[Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
public sealed class CourseMaterialController(
    CourseMaterialService materialService,
    IValidator<UpdateCourseMaterialRequest> validator,
    IValidator<UpdateCourseMaterialMetadataRequest> metadataValidator) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CourseMaterialDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseMaterialDto>> Update(
        Guid id,
        UpdateCourseMaterialRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await materialService.UpdateAsync(id, GetUserId(), IsPrivileged(), request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Ok(outcome.Value);
        }

        return outcome.FailureKind == CourseMaterialFailureKind.NotFound
            ? NotFound(new ProblemDetails { Title = outcome.Error })
            : ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.CourseSessionId)] = [outcome.Error ?? "The material request is invalid."],
            }));
    }

    [HttpPut("{id:guid}/metadata")]
    [ProducesResponseType<CourseMaterialDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseMaterialDto>> UpdateMetadata(
        Guid id,
        UpdateCourseMaterialMetadataRequest request,
        CancellationToken cancellationToken)
    {
        await metadataValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await materialService.UpdateMetadataAsync(
            id, GetUserId(), IsPrivileged(), request, cancellationToken);

        return outcome.Succeeded
            ? Ok(outcome.Value)
            : outcome.FailureKind == CourseMaterialFailureKind.NotFound
                ? NotFound(new ProblemDetails { Title = outcome.Error })
                : ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.CourseSessionId)] = [outcome.Error ?? "The material request is invalid."],
                }));
    }

    [HttpGet("{id:guid}/file")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await materialService.GetStaffFileAsync(
            id, GetUserId(), IsPrivileged(), cancellationToken);
        return outcome.Succeeded
            ? File(outcome.Value!.Content, outcome.Value.ContentType, outcome.Value.OriginalFileName)
            : NotFound(new ProblemDetails { Title = outcome.Error });
    }

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
