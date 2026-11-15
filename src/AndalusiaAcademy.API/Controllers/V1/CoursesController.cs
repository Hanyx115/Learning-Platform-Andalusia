using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
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
[Route("api/v{version:apiVersion}/courses")]
public sealed class CoursesController(
    CourseService courseService,
    OfferingService offeringService,
    IValidator<CreateCourseRequest> createValidator,
    IValidator<UpdateCourseRequest> updateValidator,
    IValidator<CourseContentRequest> contentValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginatedResult<CourseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetCourses(
        [FromQuery] CourseCatalogQuery query,
        CancellationToken cancellationToken)
    {
        var courses = await courseService.GetCoursesAsync(query, cancellationToken);
        return Ok(courses);
    }

    [HttpGet("manage")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetCoursesForManagement(
        [FromQuery] CourseCatalogQuery query,
        CancellationToken cancellationToken)
        => Ok(await courseService.GetCoursesForManagementAsync(query, cancellationToken));

    [HttpGet("{slug}")]
    [ProducesResponseType<CourseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourseBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var course = await courseService.GetCourseBySlugAsync(slug, cancellationToken);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpGet("{slug}/offerings")]
    [ProducesResponseType<IReadOnlyList<OfferingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OfferingDto>>> GetOpenOfferings(
        string slug,
        CancellationToken cancellationToken)
    {
        var offerings = await offeringService.GetOpenCourseOfferingsAsync(slug, cancellationToken);
        return Ok(offerings);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<CourseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await courseService.CreateAsync(request, cancellationToken);

        if (outcome.Succeeded)
        {
            return Created($"/api/v1/courses/{outcome.Value!.Slug}", outcome.Value);
        }

        return ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPost("content")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    [ProducesResponseType<CourseDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseDto>> CreateContent(
        CourseContentRequest request,
        CancellationToken cancellationToken)
    {
        await contentValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await courseService.CreateContentAsync(request, cancellationToken);
        return outcome.Succeeded
            ? Created($"/api/v1/courses/{outcome.Value!.Slug}", outcome.Value)
            : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<CourseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDto>> Update(
        Guid id, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await courseService.UpdateAsync(id, request, cancellationToken);

        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    [HttpPut("{id:guid}/content")]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    [ProducesResponseType<CourseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> UpdateContent(
        Guid id,
        CourseContentRequest request,
        CancellationToken cancellationToken)
    {
        await contentValidator.ValidateAndThrowAsync(request, cancellationToken);
        var outcome = await courseService.UpdateContentAsync(id, request, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : ToFailureResult(outcome.FailureKind, outcome.Error);
    }

    private ActionResult ToFailureResult(SchedulingFailureKind? kind, string? error)
        => kind switch
        {
            SchedulingFailureKind.NotFound => NotFound(new ProblemDetails { Title = error }),
            SchedulingFailureKind.Conflict => Conflict(new ProblemDetails { Title = error }),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [error ?? "The course request is invalid."],
            })),
        };
}
