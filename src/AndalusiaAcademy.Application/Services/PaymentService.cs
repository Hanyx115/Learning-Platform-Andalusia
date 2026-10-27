using AndalusiaAcademy.Application.DTOs.Payments;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class PaymentService(IPaymentRepository paymentRepository)
{
    public Task<PayForApplicationOutcome> PayAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken)
        => paymentRepository.PayForApplicationAsync(applicationId, userId, cancellationToken);

    public Task<IReadOnlyList<PaymentDto>> GetMyPaymentsAsync(Guid userId, CancellationToken cancellationToken)
        => paymentRepository.GetForUserAsync(userId, cancellationToken);

    public Task<IReadOnlyList<AdminPaymentDto>> GetAllAsync(CancellationToken cancellationToken)
        => paymentRepository.GetAllAsync(cancellationToken);

    public Task<AdminPaymentActionOutcome> FailPendingAsync(
        Guid paymentId,
        Guid actingAdminUserId,
        CancellationToken cancellationToken)
        => paymentRepository.FailPendingAsync(paymentId, actingAdminUserId, cancellationToken);
}
