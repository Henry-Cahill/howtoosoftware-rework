using Microsoft.Extensions.Diagnostics.HealthChecks;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.HealthChecks;

public sealed class DbHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Healthy("Database connection OK");

            return HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
