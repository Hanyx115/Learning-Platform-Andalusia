namespace AndalusiaAcademy.Application.Enrollment;

public sealed record PaymentProcessingResult(bool Succeeded, string? ProviderReference, string? FailureReason)
{
    public static PaymentProcessingResult Success(string? providerReference) => new(true, providerReference, null);

    public static PaymentProcessingResult Failure(string reason) => new(false, null, reason);
}
