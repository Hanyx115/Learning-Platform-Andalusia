using System.Text.Json;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Notifications;

public sealed class NotificationOutbox(ApplicationDbContext dbContext, TimeProvider timeProvider) : INotificationOutbox
{
    public async Task EnqueueAsync(NotificationEventRequest request, CancellationToken cancellationToken)
    {
        if (request.RecipientUserIds.Count == 0 ||
            await dbContext.OutboxMessages.AnyAsync(message => message.EventKey == request.EventKey, cancellationToken))
        {
            return;
        }

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventKey = request.EventKey,
            EventType = request.TemplateCode,
            PayloadJson = JsonSerializer.Serialize(request),
            OccurredAtUtc = timeProvider.GetUtcNow(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
