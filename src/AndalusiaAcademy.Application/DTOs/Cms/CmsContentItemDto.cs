namespace AndalusiaAcademy.Application.DTOs.Cms;

public sealed record CmsContentItemDto(
    Guid Id,
    string ContentType,
    string Key,
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    string? ImageUrl,
    string? LinkUrl,
    Guid? RelatedUserId,
    int DisplayOrder,
    bool IsPublished,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
