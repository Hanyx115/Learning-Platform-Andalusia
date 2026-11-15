using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Notifications;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public sealed class NotificationsController(
    NotificationService notificationService,
    IValidator<UpdateNotificationTemplateRequest> templateValidator,
    IValidator<CreateAnnouncementRequest> announcementValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<NotificationListDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListDto>> GetMine(CancellationToken cancellationToken)
        => Ok(await notificationService.GetMineAsync(GetUserId(), cancellationToken));

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
        => await notificationService.MarkReadAsync(id, GetUserId(), cancellationToken)
            ? NoContent()
            : NotFound(new ProblemDetails { Title = "Notification not found." });

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpGet("templates")]
    [Authorize(Policy = PolicyNames.RequireContentManagerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<NotificationTemplateDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationTemplateDto>>> GetTemplates(
        CancellationToken cancellationToken)
        => Ok(await notificationService.GetTemplatesAsync(cancellationToken));

    [HttpPut("templates/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireContentManagerOrAdmin)]
    [ProducesResponseType<NotificationTemplateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationTemplateDto>> UpdateTemplate(
        Guid id,
        UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await templateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var updated = await notificationService.UpdateTemplateAsync(id, request, GetUserId(), cancellationToken);
        return updated is null ? NotFound(new ProblemDetails { Title = "Notification template not found." }) : Ok(updated);
    }

    [HttpPost("announcements")]
    [Authorize(Policy = PolicyNames.RequireContentManagerManagerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAnnouncement(
        CreateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await announcementValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        await notificationService.QueueAnnouncementAsync(request, cancellationToken);
        return Accepted();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
