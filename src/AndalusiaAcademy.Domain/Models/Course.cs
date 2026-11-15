namespace AndalusiaAcademy.Domain.Models;

public class Course
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public CourseCategory? Category { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
