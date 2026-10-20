namespace AndalusiaAcademy.Domain.Models;

/// <summary>
/// Named "LearnerEnrollment" rather than "Enrollment" to avoid colliding with the
/// AndalusiaAcademy.Application.Enrollment / AndalusiaAcademy.Infrastructure.Enrollment module
/// namespaces (same reasoning as LearnerApplication vs the Application project namespace).
/// The offering is snapshotted from the application so an enrollment always retains its exact intake.
/// </summary>
public class LearnerEnrollment
{
    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public LearnerApplication? Application { get; set; }

    public Guid PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public Guid UserId { get; set; }

    public Guid? CourseOfferingId { get; set; }

    public CourseOffering? CourseOffering { get; set; }

    public Guid? ProgramOfferingId { get; set; }

    public ProgramOffering? ProgramOffering { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset EnrolledAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}
