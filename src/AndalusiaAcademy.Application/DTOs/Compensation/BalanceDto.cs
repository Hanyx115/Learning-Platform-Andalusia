namespace AndalusiaAcademy.Application.DTOs.Compensation;

public sealed record BalanceDto(decimal Balance, string Currency, DateTimeOffset? UpdatedAtUtc);

public sealed record DemoBalanceCreditRequest(decimal Amount, string Method);
