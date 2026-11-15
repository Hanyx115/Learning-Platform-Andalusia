using AndalusiaAcademy.Application.DTOs.Payments;

namespace AndalusiaAcademy.Application.Enrollment;

public enum AdminPaymentActionFailure
{
    NotFound,
    Conflict,
}

public sealed record AdminPaymentActionOutcome(
    AdminPaymentDto? Payment,
    AdminPaymentActionFailure? Failure,
    string? Error)
{
    public bool Succeeded => Payment is not null;

    public static AdminPaymentActionOutcome Success(AdminPaymentDto payment) => new(payment, null, null);

    public static AdminPaymentActionOutcome Fail(AdminPaymentActionFailure failure, string error) =>
        new(null, failure, error);
}
