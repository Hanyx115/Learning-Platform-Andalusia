using AndalusiaAcademy.Application.DTOs.Compensation;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class BalanceService(ICompensationRepository repository, TimeProvider timeProvider)
{
    private static readonly HashSet<string> DemoMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Visa", "Mastercard", "Fawry",
    };

    public Task<BalanceDto> GetMineAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetBalanceAsync(userId, cancellationToken);

    public Task<BalanceDto> AddDemoCreditAsync(
        Guid userId,
        DemoBalanceCreditRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0 || request.Amount > 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Demo credit must be between 0.01 and 100,000 EGP.");
        }

        if (!DemoMethods.Contains(request.Method))
        {
            throw new ArgumentException("Choose Visa, Mastercard, or Fawry.", nameof(request.Method));
        }

        return repository.AddDemoCreditAsync(
            userId,
            decimal.Round(request.Amount, 2),
            request.Method,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}
