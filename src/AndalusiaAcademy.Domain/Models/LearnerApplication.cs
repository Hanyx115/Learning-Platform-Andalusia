namespace AndalusiaAcademy.Domain.Models;

/// <summary>
/// Named "LearnerApplication" rather than "Application" to avoid colliding with the
/// AndalusiaAcademy.Application project namespace throughout the codebase.
/// Exactly one of <see cref="CourseId"/>/<see cref="ProgramId"/> is set (enforced by a DB check constraint).
/// </summary>
public class LearnerApplication
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid? ProgramId { get; set; }

    public AcademyProgram? Program { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public string? RejectionReason { get; set; }
}
