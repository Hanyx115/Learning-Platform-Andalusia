namespace AndalusiaAcademy.Domain.Models;

public sealed class CmsContentItem
{
    public Guid Id { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string TitleEn { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    public string BodyEn { get; set; } = string.Empty;

    public string BodyAr { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? LinkUrl { get; set; }

    public Guid? RelatedUserId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
