namespace AndalusiaAcademy.Domain.Models;

public class PaymentStatusHistory
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>E.g. "Simulated" today, "Webhook" or "Learner" once Paymob integration (#7) lands.</summary>
    public string Source { get; set; } = string.Empty;
}
