namespace AndalusiaAcademy.Domain.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public LearnerApplication? Application { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public Guid IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<PaymentStatusHistory> StatusHistory { get; set; } = [];
}
