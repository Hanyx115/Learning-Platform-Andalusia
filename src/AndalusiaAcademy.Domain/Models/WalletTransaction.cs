namespace AndalusiaAcademy.Domain.Models;

public sealed class WalletTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EGP";

    public string Type { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
