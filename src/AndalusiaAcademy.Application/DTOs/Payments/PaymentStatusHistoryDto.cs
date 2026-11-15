namespace AndalusiaAcademy.Application.DTOs.Payments;

public sealed record PaymentStatusHistoryDto(
    Guid Id,
    string Status,
    DateTimeOffset ChangedAt,
    string Source);
