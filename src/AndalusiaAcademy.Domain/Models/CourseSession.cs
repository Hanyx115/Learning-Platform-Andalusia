namespace AndalusiaAcademy.Domain.Models;

public sealed class CourseSession
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public CourseOffering? CourseOffering { get; set; }

    public Guid? LocationId { get; set; }

    public TrainingLocation? Location { get; set; }

    public Guid? InstructorUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }

    public string? ReschedulingNote { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
