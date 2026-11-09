namespace AndalusiaAcademy.Domain.Models;

public sealed class CommunityReply
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public CommunityPost? Post { get; set; }

    public Guid AuthorUserId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
}
