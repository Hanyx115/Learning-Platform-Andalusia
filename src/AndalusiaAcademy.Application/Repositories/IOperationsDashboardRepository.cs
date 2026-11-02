using AndalusiaAcademy.Application.DTOs.Operations;

namespace AndalusiaAcademy.Application.Repositories;

public interface IOperationsDashboardRepository
{
    Task<OperationsOverviewDto> GetOverviewAsync(
        DateTimeOffset now,
        int recentActivityLimit,
        CancellationToken cancellationToken);
}
