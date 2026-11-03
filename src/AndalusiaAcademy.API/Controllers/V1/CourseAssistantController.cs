using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Assistant;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/learner/enrollments/{enrollmentId:guid}/assistant/messages")]
[Authorize(Policy = PolicyNames.RequireLearner)]
[EnableRateLimiting("course-assistant")]
public sealed class CourseAssistantController(
    CourseAssistantService assistantService,
    IValidator<CourseAssistantRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CourseAssistantResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CourseAssistantResponseDto>> Ask(
        Guid enrollmentId,
        CourseAssistantRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await assistantService.AskAsync(
            enrollmentId,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            request,
            cancellationToken);

        if (outcome.Succeeded)
        {
            return Ok(outcome.Value);
        }

        return outcome.FailureKind == CourseAssistantFailureKind.NotFound
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = outcome.Error,
            })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = outcome.Error,
            });
    }
}
