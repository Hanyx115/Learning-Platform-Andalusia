using AndalusiaAcademy.Application.Enrollment;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Services;

/// <summary>
/// Stands in for a real payment gateway until #7 (Paymob) is built: resolves every payment as
/// succeeded immediately, no external call. Swap for a PaymobPaymentProcessor via DI registration
/// in <c>DependencyInjection.cs</c> when that's ready; nothing else in the Payment flow changes.
/// </summary>
public sealed class SimulatedPaymentProcessor : IPaymentProcessor
{
    public Task<PaymentProcessingResult> ProcessAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
        => Task.FromResult(PaymentProcessingResult.Success(providerReference: null));
}
