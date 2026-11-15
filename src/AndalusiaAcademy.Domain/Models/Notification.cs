namespace AndalusiaAcademy.Domain.Models;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? TemplateId { get; set; }

    public NotificationTemplate? Template { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string SourceEventKey { get; set; } = string.Empty;

    public string TitleEn { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    public string MessageEn { get; set; } = string.Empty;

    public string MessageAr { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }

    public ICollection<NotificationDelivery> Deliveries { get; set; } = [];
}
