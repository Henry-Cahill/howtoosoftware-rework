using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HowToSoftware.Admin.Hubs;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Admin.Services;

/// <summary>
/// Background service that polls the database every 10 seconds for
/// live visitor count (distinct sessions in last 5 min) and recent
/// pageviews, then pushes updates to connected admin dashboard clients
/// via SignalR.
/// </summary>
public sealed class LiveAnalyticsService(
    IServiceScopeFactory scopeFactory,
    IHubContext<AnalyticsHub> hubContext,
    ILogger<LiveAnalyticsService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LiveWindow = TimeSpan.FromMinutes(5);
    private const int RecentPageviewLimit = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PushLiveStatsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to push live analytics");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PushLiveStatsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow.Subtract(LiveWindow);

        // Live visitor count: distinct sessions in last 5 minutes
        var liveVisitors = await db.AnalyticsEvents
            .Where(e => e.Timestamp >= cutoff && e.SessionId != null)
            .Select(e => e.SessionId)
            .Distinct()
            .CountAsync(ct);

        await hubContext.Clients.All.SendAsync("ReceiveLiveVisitors", liveVisitors, ct);

        // Recent pageviews: last N events
        var recentPageviews = await db.AnalyticsEvents
            .Where(e => e.Timestamp >= cutoff && e.PageUrlPath != null)
            .OrderByDescending(e => e.Timestamp)
            .Take(RecentPageviewLimit)
            .Select(e => new RecentPageviewDto
            {
                PagePath = e.PageUrlPath!,
                Timestamp = e.Timestamp,
                Country = e.Country,
                Device = e.Device,
                Browser = e.Browser,
                Referrer = e.Referrer,
                MemberUuid = e.MemberUuid,
            })
            .ToListAsync(ct);

        await hubContext.Clients.All.SendAsync("ReceiveRecentPageviews", recentPageviews, ct);
    }
}

public sealed class RecentPageviewDto
{
    public string PagePath { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Country { get; set; }
    public string? Device { get; set; }
    public string? Browser { get; set; }
    public string? Referrer { get; set; }
    public string? MemberUuid { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
