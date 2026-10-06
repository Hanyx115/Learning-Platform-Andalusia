using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class CoursesController(CourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(CancellationToken cancellationToken)
    {
        var courses = await courseService.GetCoursesAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCourseBySlug(string slug, CancellationToken cancellationToken)
    {
        var course = await courseService.GetCourseBySlugAsync(slug, cancellationToken);
        return course is null ? NotFound() : Ok(course);
    }
}
