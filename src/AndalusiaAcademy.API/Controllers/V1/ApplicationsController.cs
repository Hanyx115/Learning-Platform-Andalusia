using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/applications")]
[Authorize]
public sealed class ApplicationsController(
    ApplicationSubmissionService applicationService,
    IValidator<CreateApplicationRequest> createValidator,
    IValidator<RejectApplicationRequest> rejectValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireLearner)]
    [ProducesResponseType<ApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplicationDto>> Submit(
        CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await applicationService.SubmitAsync(GetUserId(), request, cancellationToken);

        if (!outcome.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = outcome.Errors.ToArray(),
            }));
        }

        return Ok(outcome.Application);
    }

    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<ApplicationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApplicationDto>>> GetMine(CancellationToken cancellationToken)
    {
        var applications = await applicationService.GetMyApplicationsAsync(GetUserId(), cancellationToken);
        return Ok(applications);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<PaginatedResult<ApplicationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ApplicationDto>>> GetPage(
        [FromQuery] ApplicationListQuery query,
        CancellationToken cancellationToken)
    {
        var applications = await applicationService.GetPageAsync(query, cancellationToken);
        return Ok(applications);
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<ApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> Accept(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await applicationService.AcceptAsync(id, GetUserId(), cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Application) : NotFound(new ProblemDetails { Title = outcome.Error });
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<ApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplicationDto>> Reject(
        Guid id,
        RejectApplicationRequest request,
        CancellationToken cancellationToken)
    {
        await rejectValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await applicationService.RejectAsync(id, GetUserId(), request.Reason, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Application) : NotFound(new ProblemDetails { Title = outcome.Error });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
