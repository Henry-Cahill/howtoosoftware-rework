using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using HowToSoftware.Infrastructure.Services;
using Stripe;

namespace HowToSoftware.Infrastructure.HealthChecks;

public sealed class StripeHealthCheck(IOptions<StripeSettings> settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var key = settings.Value.SecretKey;
        if (string.IsNullOrWhiteSpace(key))
            return HealthCheckResult.Degraded("Stripe secret key not configured");

        try
        {
            var service = new BalanceService(new StripeClient(key));
            await service.GetAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Stripe API reachable");
        }
        catch (StripeException ex)
        {
            return HealthCheckResult.Unhealthy("Stripe API error", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Stripe connectivity check failed", ex);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
