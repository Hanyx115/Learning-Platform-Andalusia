using AndalusiaAcademy.Application.DTOs.Compensation;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICompensationRepository
{
    Task<BalanceDto> GetBalanceAsync(Guid userId, CancellationToken cancellationToken);

    Task<BalanceDto> AddDemoCreditAsync(Guid userId, decimal amount, string method, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<InstructorCompensationTargetDto>> GetInstructorTargetsAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> SetInstructorRateAsync(SetInstructorCourseRateRequest request, Guid managerUserId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<StaffCompensationRateDto>> GetStaffRatesAsync(CancellationToken cancellationToken);

    Task<bool> SetStaffRateAsync(Guid userId, decimal amount, string currency, Guid adminUserId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<CompensationPayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken);

    Task<CompensationActionOutcome> CreateInstructorPayoutAsync(Guid rateId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<CompensationActionOutcome> CreateStaffPayoutAsync(Guid userId, DateOnly periodMonth, DateTimeOffset now, CancellationToken cancellationToken);

    Task<CompensationActionOutcome> ReviewPayoutAsync(Guid payoutId, Guid managerUserId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<CompensationActionOutcome> ApprovePayoutAsync(Guid payoutId, Guid adminUserId, DateTimeOffset now, CancellationToken cancellationToken);
}
