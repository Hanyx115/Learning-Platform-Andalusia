using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AndalusiaAcademy.API.HealthChecks;

public sealed class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("The API is running."));
}
