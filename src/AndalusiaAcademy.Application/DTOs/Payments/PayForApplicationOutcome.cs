using AndalusiaAcademy.Application.DTOs.Enrollments;

namespace AndalusiaAcademy.Application.DTOs.Payments;

public sealed record PayForApplicationOutcome(
    bool Succeeded,
    PaymentDto? Payment,
    EnrollmentDto? Enrollment,
    string? Error)
{
    public static PayForApplicationOutcome Success(PaymentDto payment, EnrollmentDto enrollment) =>
        new(true, payment, enrollment, null);

    public static PayForApplicationOutcome Failure(string error) => new(false, null, null, error);
}
