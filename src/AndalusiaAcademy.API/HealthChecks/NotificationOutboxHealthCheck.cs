using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AndalusiaAcademy.API.HealthChecks;

public sealed class NotificationOutboxHealthCheck(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Notifications:ProcessingEnabled", true))
        {
            return HealthCheckResult.Healthy("Notification processing is intentionally disabled.");
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pendingQuery = dbContext.OutboxMessages
                .AsNoTracking()
                .Where(message => message.ProcessedAtUtc == null);

            var pendingCount = await pendingQuery.CountAsync(cancellationToken);
            var oldestPendingAt = await pendingQuery
                .OrderBy(message => message.OccurredAtUtc)
                .Select(message => (DateTimeOffset?)message.OccurredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var highestAttemptCount = await pendingQuery
                .Select(message => (int?)message.AttemptCount)
                .MaxAsync(cancellationToken) ?? 0;

            var data = new Dictionary<string, object>
            {
                ["pendingCount"] = pendingCount,
                ["highestAttemptCount"] = highestAttemptCount,
            };
            if (oldestPendingAt.HasValue)
            {
                data["oldestPendingAgeSeconds"] = Math.Max(
                    0,
                    (timeProvider.GetUtcNow() - oldestPendingAt.Value).TotalSeconds);
            }

            var unhealthyAttempts = configuration.GetValue("Notifications:Health:UnhealthyAttemptCount", 5);
            if (highestAttemptCount >= unhealthyAttempts)
            {
                return HealthCheckResult.Unhealthy(
                    "Notification delivery has repeatedly failed.",
                    data: data);
            }

            var degradedPendingCount = configuration.GetValue("Notifications:Health:DegradedPendingCount", 100);
            var degradedAgeMinutes = configuration.GetValue("Notifications:Health:DegradedOldestMinutes", 5);
            if (pendingCount >= degradedPendingCount ||
                oldestPendingAt < timeProvider.GetUtcNow().AddMinutes(-degradedAgeMinutes))
            {
                return HealthCheckResult.Degraded("Notification processing is backlogged.", data: data);
            }

            return HealthCheckResult.Healthy("Notification processing is current.", data);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Notification readiness check failed.", exception);
        }
    }
}
