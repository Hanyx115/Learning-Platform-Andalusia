using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/cms/content")]
[Authorize(Policy = PolicyNames.RequireContentManagerOrAdmin)]
public sealed class CmsController(
    CmsService cmsService,
    IValidator<UpsertCmsContentItemRequest> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CmsContentItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CmsContentItemDto>>> GetAll(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
        => Ok(await cmsService.GetAllAsync(type, cancellationToken));

    [HttpPost]
    [ProducesResponseType<CmsContentItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CmsContentItemDto>> Create(
        UpsertCmsContentItemRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var outcome = await cmsService.CreateAsync(request, GetUserId(), cancellationToken);
        return outcome.Succeeded
            ? CreatedAtAction(nameof(GetAll), new { version = "1" }, outcome.Value)
            : Conflict(new ProblemDetails { Title = outcome.Error });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CmsContentItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CmsContentItemDto>> Update(
        Guid id,
        UpsertCmsContentItemRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var outcome = await cmsService.UpdateAsync(id, request, GetUserId(), cancellationToken);
        return outcome.Succeeded
            ? Ok(outcome.Value)
            : Conflict(new ProblemDetails { Title = outcome.Error });
    }

    [HttpPatch("{id:guid}/publication")]
    [ProducesResponseType<CmsContentItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CmsContentItemDto>> SetPublication(
        Guid id,
        SetCmsPublicationRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await cmsService.SetPublicationAsync(id, request.IsPublished, GetUserId(), cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Value) : NotFound(new ProblemDetails { Title = outcome.Error });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await cmsService.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound(new ProblemDetails { Title = "Content item not found." });

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
