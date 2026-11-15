namespace AndalusiaAcademy.Application.DTOs.Notifications;

public sealed record NotificationTemplateDto(
    Guid Id,
    string Code,
    string TitleEn,
    string TitleAr,
    string MessageEn,
    string MessageAr,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);
