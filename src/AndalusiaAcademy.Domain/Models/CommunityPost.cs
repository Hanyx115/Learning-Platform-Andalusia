namespace AndalusiaAcademy.Domain.Models;

public sealed class CommunityPost
{
    public Guid Id { get; set; }

    public Guid? CourseOfferingId { get; set; }

    public CourseOffering? CourseOffering { get; set; }

    public Guid? ProgramOfferingId { get; set; }

    public ProgramOffering? ProgramOffering { get; set; }

    public Guid AuthorUserId { get; set; }

    public string PostType { get; set; } = CommunityPostTypes.Discussion;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    public bool IsLocked { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
}

public static class CommunityPostTypes
{
    public const string Discussion = "Discussion";
    public const string Announcement = "Announcement";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Discussion,
        Announcement,
    };
}
