using AndalusiaAcademy.Application.DTOs;
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
[Route("api/v{version:apiVersion}/course-categories")]
public sealed class CourseCategoriesController(
    CourseCategoryService categoryService,
    IValidator<CreateCourseCategoryRequest> createValidator,
    IValidator<UpdateCourseCategoryRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseCategoryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await categoryService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<CourseCategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseCategoryDto>> Create(
        CreateCourseCategoryRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await categoryService.CreateAsync(request, cancellationToken);

        return outcome.Succeeded
            ? Created($"/api/v1/course-categories/{outcome.Value!.Id}", outcome.Value)
            : Conflict(new ProblemDetails { Title = outcome.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<CourseCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseCategoryDto>> Update(
        Guid id, UpdateCourseCategoryRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await categoryService.UpdateAsync(id, request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Ok(outcome.Value);
        }

        return outcome.FailureKind == SchedulingFailureKind.NotFound
            ? NotFound(new ProblemDetails { Title = outcome.Error })
            : Conflict(new ProblemDetails { Title = outcome.Error });
    }
}
