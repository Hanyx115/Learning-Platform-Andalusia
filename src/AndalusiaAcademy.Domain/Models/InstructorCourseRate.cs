namespace AndalusiaAcademy.Domain.Models;

public sealed class InstructorCourseRate
{
    public Guid Id { get; set; }

    public Guid InstructorUserId { get; set; }

    public Guid CourseOfferingId { get; set; }

    public CourseOffering? CourseOffering { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EGP";

    public Guid SetByManagerUserId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
