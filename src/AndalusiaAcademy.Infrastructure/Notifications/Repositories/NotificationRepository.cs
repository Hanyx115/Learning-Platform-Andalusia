using AndalusiaAcademy.Application.DTOs.Notifications;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Notifications.Repositories;

public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task<NotificationListDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(100)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.EventType,
                notification.TitleEn,
                notification.TitleAr,
                notification.MessageEn,
                notification.MessageAr,
                notification.LinkUrl,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ToListAsync(cancellationToken);

        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && notification.ReadAtUtc == null, cancellationToken);

        return new NotificationListDto(items, unreadCount);
    }

    public async Task<bool> MarkReadAsync(
        Guid id,
        Guid userId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.Notifications
            .Where(notification => notification.Id == id && notification.UserId == userId && notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAtUtc, readAtUtc), cancellationToken);

        return updated > 0 || await dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(notification => notification.Id == id && notification.UserId == userId, cancellationToken);
    }

    public Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset readAtUtc, CancellationToken cancellationToken)
        => dbContext.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAtUtc, readAtUtc), cancellationToken);

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
        => await dbContext.NotificationTemplates
            .AsNoTracking()
            .OrderBy(template => template.Code)
            .Select(template => new NotificationTemplateDto(
                template.Id,
                template.Code,
                template.TitleEn,
                template.TitleAr,
                template.MessageEn,
                template.MessageAr,
                template.IsActive,
                template.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

    public Task<NotificationTemplate?> GetTemplateForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.NotificationTemplates.FirstOrDefaultAsync(template => template.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
