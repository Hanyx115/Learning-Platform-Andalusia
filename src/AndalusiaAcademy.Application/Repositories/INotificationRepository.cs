using AndalusiaAcademy.Application.DTOs.Notifications;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface INotificationRepository
{
    Task<NotificationListDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(Guid id, Guid userId, DateTimeOffset readAtUtc, CancellationToken cancellationToken);

    Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset readAtUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetTemplateForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
