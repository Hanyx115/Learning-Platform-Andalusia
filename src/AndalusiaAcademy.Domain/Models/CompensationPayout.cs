namespace AndalusiaAcademy.Domain.Models;

public sealed class CompensationPayout
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PayoutType { get; set; } = string.Empty;

    public Guid? InstructorCourseRateId { get; set; }

    public InstructorCourseRate? InstructorCourseRate { get; set; }

    public DateOnly? PeriodMonth { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EGP";

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid? ReviewedByManagerUserId { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    public Guid? ApprovedByAdminUserId { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }
}
