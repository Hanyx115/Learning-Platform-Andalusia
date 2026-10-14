namespace AndalusiaAcademy.Application.DTOs.Payments;

public sealed record PaymentDto(
    Guid Id,
    Guid ApplicationId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
