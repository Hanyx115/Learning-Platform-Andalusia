namespace AndalusiaAcademy.Domain.Models;

public sealed class StaffCompensationRate
{
    public Guid UserId { get; set; }

    public decimal MonthlyAmount { get; set; }

    public string Currency { get; set; } = "EGP";

    public Guid SetByAdminUserId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
