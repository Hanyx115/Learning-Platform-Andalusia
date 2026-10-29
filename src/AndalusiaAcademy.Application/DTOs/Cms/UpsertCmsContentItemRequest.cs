namespace AndalusiaAcademy.Application.DTOs.Cms;

public sealed record UpsertCmsContentItemRequest(
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
    bool IsPublished);
