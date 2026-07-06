using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Services;

/// <summary>
/// Background service that polls <c>automated_email_schedules</c> for queued
/// drip / delayed automated emails whose <c>ScheduledFor</c> has elapsed and
/// dispatches each via <see cref="IAutomatedEmailService.DispatchScheduledAsync"/>.
/// </summary>
public sealed class AutomatedEmailDripService(
    IServiceScopeFactory scopeFactory,
    ILogger<AutomatedEmailDripService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Automated email drip dispatcher encountered an error");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchDueAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAutomatedEmailRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedEmailService>();

        var due = await repo.GetDueSchedulesAsync(DateTime.UtcNow, BatchSize, ct);
        if (due.Count == 0)
            return;

        logger.LogInformation("Dispatching {Count} scheduled automated email(s)", due.Count);

        foreach (var schedule in due)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await service.DispatchScheduledAsync(schedule.Id, ct);
            }
            catch (Exception ex)
            {
                // DispatchScheduledAsync already marks the row processed +
                // records the failure reason on exception, so this catch is
                // a safety net to keep the loop alive for the rest of the batch.
                logger.LogError(ex,
                    "Unhandled error dispatching automated email schedule {ScheduleId}",
                    schedule.Id);
            }
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
