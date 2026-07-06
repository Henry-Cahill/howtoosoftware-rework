using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HowToSoftware.Infrastructure.HealthChecks;

public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private const double DegradedThresholdGb = 2.0;
    private const double UnhealthyThresholdGb = 0.5;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var rootPath = Path.GetPathRoot(AppContext.BaseDirectory) ?? "/";
            var drive = new DriveInfo(rootPath);
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);

            var data = new Dictionary<string, object>
            {
                ["drive"] = drive.Name,
                ["freeGb"] = Math.Round(freeGb, 2),
                ["totalGb"] = Math.Round(drive.TotalSize / (1024.0 * 1024 * 1024), 2)
            };

            if (freeGb < UnhealthyThresholdGb)
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Critically low disk: {freeGb:F1} GB free", data: data));

            if (freeGb < DegradedThresholdGb)
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {freeGb:F1} GB free", data: data));

            return Task.FromResult(HealthCheckResult.Healthy(
                $"{freeGb:F1} GB free", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
