using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

/// <summary>
/// Background service that periodically aggregates raw analytics_events
/// into hourly and daily summary tables via stored procedures.
/// Runs every hour: rolls up the previous hour, and (once per day at the
/// first run after midnight UTC) rolls up the previous day.
/// </summary>
public sealed class AnalyticsRollupService(
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsRollupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a short delay on startup to let the app fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRollupsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analytics rollup failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunRollupsAsync(CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;

        // Hourly rollup: aggregate the previous completed hour
        var previousHour = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc)
            .AddHours(-1);

        await ExecuteRollupAsync("usp_RollupHourly", "@BucketHour", previousHour, ct);
        logger.LogInformation("Hourly rollup completed for {BucketHour}", previousHour);

        // Daily rollup: aggregate the previous completed day
        // Run on every cycle so the data stays fresh — the MERGE upserts safely
        var previousDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(-1);

        await ExecuteRollupAsync("usp_RollupDaily", "@BucketDate", previousDay, ct);
        logger.LogInformation("Daily rollup completed for {BucketDate:yyyy-MM-dd}", previousDay);
    }

    private async Task ExecuteRollupAsync(string procedureName, string paramName, DateTime paramValue, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"EXEC dbo.{procedureName} {paramName}";
        cmd.CommandType = System.Data.CommandType.Text;

        var param = cmd.CreateParameter();
        param.ParameterName = paramName;
        param.Value = paramValue;
        param.DbType = System.Data.DbType.DateTime2;
        cmd.Parameters.Add(param);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
