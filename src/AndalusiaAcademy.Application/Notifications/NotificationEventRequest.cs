namespace AndalusiaAcademy.Application.Notifications;

public sealed record NotificationEventRequest(
    string EventKey,
    string TemplateCode,
    IReadOnlyList<Guid> RecipientUserIds,
    IReadOnlyDictionary<string, string> Tokens,
    string? LinkUrl = null,
    string? TitleEn = null,
    string? TitleAr = null,
    string? MessageEn = null,
    string? MessageAr = null);
