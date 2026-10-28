namespace AndalusiaAcademy.Application.Notifications;

public interface INotificationOutbox
{
    Task EnqueueAsync(NotificationEventRequest request, CancellationToken cancellationToken);
}
