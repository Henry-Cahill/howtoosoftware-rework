using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using HowToSoftware.Infrastructure.Services;

namespace HowToSoftware.Infrastructure.HealthChecks;

public sealed class MailgunHealthCheck(IOptions<MailSettings> settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cfg = settings.Value;
        if (string.IsNullOrWhiteSpace(cfg.SmtpHost) || string.IsNullOrWhiteSpace(cfg.MailgunApiKey))
            return HealthCheckResult.Degraded("Mailgun SMTP not configured");

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
            var username = string.IsNullOrWhiteSpace(cfg.SmtpUsername)
                ? $"postmaster@{cfg.MailgunDomain}"
                : cfg.SmtpUsername;
            await client.AuthenticateAsync(username, cfg.MailgunApiKey, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
            return HealthCheckResult.Healthy("Mailgun SMTP reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Mailgun SMTP check failed", ex);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
