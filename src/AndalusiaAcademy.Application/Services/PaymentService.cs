using AndalusiaAcademy.Application.DTOs.Payments;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class PaymentService(IPaymentRepository paymentRepository)
{
    public Task<PayForApplicationOutcome> PayAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken)
        => paymentRepository.PayForApplicationAsync(applicationId, userId, cancellationToken);

    public Task<IReadOnlyList<PaymentDto>> GetMyPaymentsAsync(Guid userId, CancellationToken cancellationToken)
        => paymentRepository.GetForUserAsync(userId, cancellationToken);
}
