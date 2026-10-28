namespace AndalusiaAcademy.Domain.Models;

public sealed class NotificationTemplate
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string TitleEn { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    public string MessageEn { get; set; } = string.Empty;

    public string MessageAr { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
