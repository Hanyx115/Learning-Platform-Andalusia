namespace AndalusiaAcademy.Domain.Models;

public sealed class WalletAccount
{
    public Guid UserId { get; set; }

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "EGP";

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
