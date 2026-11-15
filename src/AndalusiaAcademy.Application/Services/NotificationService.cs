using AndalusiaAcademy.Application.DTOs.Notifications;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class NotificationService(
    INotificationRepository repository,
    INotificationOutbox outbox,
    TimeProvider timeProvider)
{
    public Task<NotificationListDto> GetMineAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetForUserAsync(userId, cancellationToken);

    public Task<bool> MarkReadAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        => repository.MarkReadAsync(id, userId, timeProvider.GetUtcNow(), cancellationToken);

    public Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
        => repository.MarkAllReadAsync(userId, timeProvider.GetUtcNow(), cancellationToken);

    public Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
        => repository.GetTemplatesAsync(cancellationToken);

    public async Task<NotificationTemplateDto?> UpdateTemplateAsync(
        Guid id,
        UpdateNotificationTemplateRequest request,
        Guid actingUserId,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateForUpdateAsync(id, cancellationToken);
        if (template is null)
        {
            return null;
        }

        template.TitleEn = request.TitleEn.Trim();
        template.TitleAr = request.TitleAr.Trim();
        template.MessageEn = request.MessageEn.Trim();
        template.MessageAr = request.MessageAr.Trim();
        template.IsActive = request.IsActive;
        template.UpdatedAtUtc = timeProvider.GetUtcNow();
        template.UpdatedByUserId = actingUserId;
        await repository.SaveChangesAsync(cancellationToken);

        return new NotificationTemplateDto(
            template.Id,
            template.Code,
            template.TitleEn,
            template.TitleAr,
            template.MessageEn,
            template.MessageAr,
            template.IsActive,
            template.UpdatedAtUtc);
    }

    public async Task QueueAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        await outbox.EnqueueAsync(
            new NotificationEventRequest(
                $"announcement:{Guid.NewGuid():N}",
                NotificationTemplateCodes.CourseAnnouncement,
                request.RecipientUserIds.Distinct().ToArray(),
                new Dictionary<string, string>(),
                NormalizeOptional(request.LinkUrl),
                request.TitleEn.Trim(),
                request.TitleAr.Trim(),
                request.MessageEn.Trim(),
                request.MessageAr.Trim()),
            cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
