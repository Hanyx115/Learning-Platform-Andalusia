namespace AndalusiaAcademy.Domain.Models;

public sealed class CourseOffering
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid LocationId { get; set; }

    public TrainingLocation? Location { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public DateTimeOffset ApplicationOpensAtUtc { get; set; }

    public DateTimeOffset ApplicationClosesAtUtc { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
