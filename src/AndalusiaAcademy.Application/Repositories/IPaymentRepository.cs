using AndalusiaAcademy.Application.DTOs.Payments;

namespace AndalusiaAcademy.Application.Repositories;

public interface IPaymentRepository
{
    /// <summary>
    /// Validates ownership and that the application is Accepted, runs the configured
    /// <c>IPaymentProcessor</c>, and on success creates the Payment and Enrollment atomically
    /// (a single transaction) so the two can never end up out of sync.
    /// </summary>
    Task<PayForApplicationOutcome> PayForApplicationAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminPaymentDto>> GetAllAsync(CancellationToken cancellationToken);
}
