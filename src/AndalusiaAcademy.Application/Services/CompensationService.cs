using AndalusiaAcademy.Application.DTOs.Compensation;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CompensationService(ICompensationRepository repository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<InstructorCompensationTargetDto>> GetInstructorTargetsAsync(CancellationToken cancellationToken)
        => repository.GetInstructorTargetsAsync(timeProvider.GetUtcNow(), cancellationToken);

    public async Task SetInstructorRateAsync(
        SetInstructorCourseRateRequest request,
        Guid managerUserId,
        CancellationToken cancellationToken)
    {
        ValidateAmountAndCurrency(request.Amount, request.Currency);
        if (!await repository.SetInstructorRateAsync(request with { Currency = NormalizeCurrency(request.Currency) }, managerUserId, timeProvider.GetUtcNow(), cancellationToken))
        {
            throw new InvalidOperationException("The instructor is not assigned to this course offering.");
        }
    }

    public Task<IReadOnlyList<StaffCompensationRateDto>> GetStaffRatesAsync(CancellationToken cancellationToken)
        => repository.GetStaffRatesAsync(cancellationToken);

    public async Task SetStaffRateAsync(
        Guid userId,
        SetStaffMonthlyRateRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        ValidateAmountAndCurrency(request.Amount, request.Currency);
        if (!await repository.SetStaffRateAsync(userId, decimal.Round(request.Amount, 2), NormalizeCurrency(request.Currency), adminUserId, timeProvider.GetUtcNow(), cancellationToken))
        {
            throw new InvalidOperationException("Monthly compensation can only be assigned to a Manager or Content Manager.");
        }
    }

    public Task<IReadOnlyList<CompensationPayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken)
        => repository.GetPayoutsAsync(cancellationToken);

    public Task<CompensationActionOutcome> CreateInstructorPayoutAsync(Guid rateId, CancellationToken cancellationToken)
        => repository.CreateInstructorPayoutAsync(rateId, timeProvider.GetUtcNow(), cancellationToken);

    public Task<CompensationActionOutcome> CreateStaffPayoutAsync(Guid userId, CreateStaffMonthlyPayoutRequest request, CancellationToken cancellationToken)
    {
        if (request.Year is < 2020 or > 2200 || request.Month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Enter a valid compensation month.");
        }

        return repository.CreateStaffPayoutAsync(
            userId,
            new DateOnly(request.Year, request.Month, 1),
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public Task<CompensationActionOutcome> ReviewPayoutAsync(Guid payoutId, Guid managerUserId, CancellationToken cancellationToken)
        => repository.ReviewPayoutAsync(payoutId, managerUserId, timeProvider.GetUtcNow(), cancellationToken);

    public Task<CompensationActionOutcome> ApprovePayoutAsync(Guid payoutId, Guid adminUserId, CancellationToken cancellationToken)
        => repository.ApprovePayoutAsync(payoutId, adminUserId, timeProvider.GetUtcNow(), cancellationToken);

    private static void ValidateAmountAndCurrency(decimal amount, string currency)
    {
        if (amount <= 0 || amount > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The compensation amount must be between 0.01 and 1,000,000.");
        }

        if (!string.Equals(currency?.Trim(), "EGP", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Phase 1 compensation uses EGP only.", nameof(currency));
        }
    }

    private static string NormalizeCurrency(string currency) => currency.Trim().ToUpperInvariant();
}
