namespace AndalusiaAcademy.Domain.Models;

/// <summary>
/// Named "LearnerEnrollment" rather than "Enrollment" to avoid colliding with the
/// AndalusiaAcademy.Application.Enrollment / AndalusiaAcademy.Infrastructure.Enrollment module
/// namespaces (same reasoning as LearnerApplication vs the Application project namespace).
/// Course/ProgramId are snapshotted from the LearnerApplication at enrollment time, so this
/// record stays correct even if the application is later changed or the catalog item is removed.
/// </summary>
public class LearnerEnrollment
{
    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public LearnerApplication? Application { get; set; }

    public Guid PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public Guid UserId { get; set; }

    public Guid? CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid? ProgramId { get; set; }

    public AcademyProgram? Program { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset EnrolledAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}
