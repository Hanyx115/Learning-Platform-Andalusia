namespace AndalusiaAcademy.Application.DTOs.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string EventType,
    string TitleEn,
    string TitleAr,
    string MessageEn,
    string MessageAr,
    string? LinkUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record NotificationListDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount);
