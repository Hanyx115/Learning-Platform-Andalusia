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
[Route("api/v{version:apiVersion}/courses/{courseId:guid}/materials")]
[Authorize(Policy = PolicyNames.RequireInstructorManagerOrAdmin)]
public sealed class CourseMaterialsController(
    CourseMaterialService materialService,
    IValidator<CreateCourseMaterialRequest> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseMaterialDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CourseMaterialDto>>> GetForManagement(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var materials = await materialService.GetForCourseManagementAsync(
            courseId, GetUserId(), IsPrivileged(), cancellationToken);

        return materials is null
            ? NotFound(new ProblemDetails { Title = "Course not found." })
            : Ok(materials);
    }

    [HttpPost]
    [ProducesResponseType<CourseMaterialDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseMaterialDto>> Create(
        Guid courseId,
        CreateCourseMaterialRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await materialService.CreateAsync(
            courseId,
            GetUserId(),
            IsPrivileged(),
            request,
            cancellationToken);

        if (outcome.Succeeded)
        {
            return Created($"/api/v1/course-materials/{outcome.Value!.Id}", outcome.Value);
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
