namespace AndalusiaAcademy.Application.DTOs.Notifications;

public sealed record CreateAnnouncementRequest(
    IReadOnlyList<Guid> RecipientUserIds,
    string TitleEn,
    string TitleAr,
    string MessageEn,
    string MessageAr,
    string? LinkUrl);
