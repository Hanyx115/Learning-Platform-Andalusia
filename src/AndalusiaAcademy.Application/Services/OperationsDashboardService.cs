using AndalusiaAcademy.Application.DTOs.Operations;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class OperationsDashboardService(
    IOperationsDashboardRepository repository,
    TimeProvider timeProvider)
{
    private const int RecentActivityLimit = 12;

    public Task<OperationsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
        => repository.GetOverviewAsync(
            timeProvider.GetUtcNow(),
            RecentActivityLimit,
            cancellationToken);
}
