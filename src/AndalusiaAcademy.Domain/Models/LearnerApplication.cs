namespace AndalusiaAcademy.Domain.Models;

/// <summary>
/// Named "LearnerApplication" rather than "Application" to avoid colliding with the
/// AndalusiaAcademy.Application project namespace throughout the codebase.
/// Exactly one of <see cref="CourseOfferingId"/>/<see cref="ProgramOfferingId"/> is set (enforced by a DB check constraint).
/// </summary>
public class LearnerApplication
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? CourseOfferingId { get; set; }

    public CourseOffering? CourseOffering { get; set; }

    public Guid? ProgramOfferingId { get; set; }

    public ProgramOffering? ProgramOffering { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public string? RejectionReason { get; set; }

    public ICollection<LearnerApplicationStatusHistory> StatusHistory { get; set; } = [];
}
