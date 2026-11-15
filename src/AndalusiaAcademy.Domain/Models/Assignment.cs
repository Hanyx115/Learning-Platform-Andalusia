namespace AndalusiaAcademy.Domain.Models;

/// <summary>Scoped to an offering (a specific cohort run), not a single session, since due dates
/// and grading are cohort-specific. Exactly one of CourseOfferingId/ProgramOfferingId is set
/// (enforced by a DB check constraint, same pattern as LearnerApplication/Payment/Enrollment).</summary>
public sealed class Assignment
{
    public Guid Id { get; set; }

    public Guid? CourseOfferingId { get; set; }

    public CourseOffering? CourseOffering { get; set; }

    public Guid? ProgramOfferingId { get; set; }

    public ProgramOffering? ProgramOffering { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset DueAtUtc { get; set; }

    public decimal MaxScore { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
