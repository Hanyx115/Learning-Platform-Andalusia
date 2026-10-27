namespace AndalusiaAcademy.Application.DTOs.Payments;

/// <summary>The admin ledger view of a payment: same fields as PaymentDto plus who paid and
/// what for, since an admin is looking across every learner rather than their own.</summary>
public sealed record AdminPaymentDto(
    Guid Id,
    Guid UserId,
    string LearnerName,
    string Email,
    Guid ApplicationId,
    string? CourseTitle,
    string? ProgramTitle,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<PaymentStatusHistoryDto> StatusHistory);
