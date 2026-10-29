using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AndalusiaAcademy.API.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMilliseconds = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                data = entry.Value.Data,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }
}
