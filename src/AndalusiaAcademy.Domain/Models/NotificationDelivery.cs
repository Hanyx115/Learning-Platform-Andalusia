namespace AndalusiaAcademy.Domain.Models;

public sealed class NotificationDelivery
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public Notification? Notification { get; set; }

    public string Channel { get; set; } = NotificationChannels.InApp;

    public string Status { get; set; } = NotificationDeliveryStatuses.Delivered;

    public int AttemptCount { get; set; }

    public DateTimeOffset? DeliveredAtUtc { get; set; }

    public string? FailureReason { get; set; }
}
