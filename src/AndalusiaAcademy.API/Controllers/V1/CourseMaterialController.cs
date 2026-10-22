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
    IValidator<UpdateCourseMaterialRequest> validator) : ControllerBase
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

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
