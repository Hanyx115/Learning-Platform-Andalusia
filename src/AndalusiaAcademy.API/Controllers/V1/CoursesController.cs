using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
public sealed class CoursesController(CourseService courseService) : ControllerBase
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
}
