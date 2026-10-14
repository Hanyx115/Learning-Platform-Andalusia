namespace AndalusiaAcademy.Application.Enrollment;

/// <summary>
/// The seam between the Payment workflow and the actual payment provider. Today
/// <c>SimulatedPaymentProcessor</c> resolves immediately with no external call; when #7 (Paymob)
/// lands, a <c>PaymobPaymentProcessor</c> implementation swaps in here (one DI registration change)
/// and typically returns "pending" instead, with the real outcome arriving later via webhook.
/// </summary>
public interface IPaymentProcessor
{
    Task<PaymentProcessingResult> ProcessAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken);
}
