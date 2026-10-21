namespace AndalusiaAcademy.Domain.Models;

public sealed class CourseMaterial
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid? CourseSessionId { get; set; }

    public CourseSession? CourseSession { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ResourceUrl { get; set; } = string.Empty;

    public string MaterialType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string Visibility { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
