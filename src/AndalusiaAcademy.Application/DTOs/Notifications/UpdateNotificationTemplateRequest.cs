namespace AndalusiaAcademy.Application.DTOs.Notifications;

public sealed record UpdateNotificationTemplateRequest(
    string TitleEn,
    string TitleAr,
    string MessageEn,
    string MessageAr,
    bool IsActive);
