using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class AnalyticsRepository(AppDbContext db) : IAnalyticsRepository
{
    public async Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken ct = default)
    {
        db.AnalyticsEvents.Add(analyticsEvent);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default)
    {
        db.AnalyticsEvents.AddRange(events);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AnalyticsEvent>> GetEventsAsync(DateTime from, DateTime to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AnalyticsEvent>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<int> GetVisitorCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.SessionId != null)
            .Select(e => e.SessionId)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<int> GetVisitCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        // With session-based tracking, visits = distinct sessions (same as visitors)
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.SessionId != null)
            .Select(e => e.SessionId)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<int> GetPageviewCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .CountAsync(ct);
    }

    public async Task<decimal> GetBounceRateAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            ;WITH session_stats AS (
                SELECT [session_id], COUNT(*) AS event_count
                FROM [dbo].[analytics_events]
                WHERE [timestamp] >= @StartDate AND [timestamp] < @EndDate
                  AND [session_id] IS NOT NULL
                GROUP BY [session_id]
            )
            SELECT
                CAST(
                    CASE WHEN COUNT(*) = 0 THEN 0
                    ELSE 100.0 * SUM(CASE WHEN event_count = 1 THEN 1 ELSE 0 END) / COUNT(*)
                    END AS DECIMAL(5,2))
            FROM session_stats;
            """;
        AddDateParams(cmd, from, to);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is decimal d ? d : 0;
    }

    public async Task<decimal> GetAvgSessionDurationAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            ;WITH session_stats AS (
                SELECT [session_id],
                       DATEDIFF(SECOND, MIN([timestamp]), MAX([timestamp])) AS duration_sec
                FROM [dbo].[analytics_events]
                WHERE [timestamp] >= @StartDate AND [timestamp] < @EndDate
                  AND [session_id] IS NOT NULL
                GROUP BY [session_id]
            )
            SELECT CAST(ISNULL(AVG(
                CASE WHEN duration_sec > 0 THEN CAST(duration_sec AS DECIMAL(10,2)) END
            ), 0) AS DECIMAL(10,2))
            FROM session_stats;
            """;
        AddDateParams(cmd, from, to);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is decimal d ? d : 0;
    }

    public async Task<AnalyticsKpiSummary> GetKpiSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC dbo.usp_KPI_Summary @StartDate, @EndDate";
        AddDateParams(cmd, from, to);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new AnalyticsKpiSummary(
                reader.GetInt32(reader.GetOrdinal("Visitors")),
                reader.GetInt32(reader.GetOrdinal("Visits")),
                reader.GetInt32(reader.GetOrdinal("Pageviews")),
                reader.GetDecimal(reader.GetOrdinal("BounceRatePercent")),
                reader.GetDecimal(reader.GetOrdinal("AvgSessionDurationSeconds")));
        }
        return new AnalyticsKpiSummary(0, 0, 0, 0, 0);
    }

    public async Task<List<(string PageUrl, int Views)>> GetTopPagesAsync(DateTime from, DateTime to, int count, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.PageUrl != null)
            .GroupBy(e => e.PageUrl!)
            .Select(g => new { PageUrl = g.Key, Views = g.Count() })
            .OrderByDescending(x => x.Views)
            .Take(count)
            .Select(x => ValueTuple.Create(x.PageUrl, x.Views))
            .ToListAsync(ct);
    }

    public async Task<List<(string Source, int Visits)>> GetTopSourcesAsync(DateTime from, DateTime to, int count, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.Referrer != null)
            .GroupBy(e => e.Referrer!)
            .Select(g => new { Source = g.Key, Visits = g.Count() })
            .OrderByDescending(x => x.Visits)
            .Take(count)
            .Select(x => ValueTuple.Create(x.Source, x.Visits))
            .ToListAsync(ct);
    }

    public async Task<List<(string Device, int Count)>> GetDeviceBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.Device != null)
            .GroupBy(e => e.Device!)
            .Select(g => new { Device = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => ValueTuple.Create(x.Device, x.Count))
            .ToListAsync(ct);
    }

    public async Task<List<(string Browser, int Count)>> GetBrowserBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.Browser != null)
            .GroupBy(e => e.Browser!)
            .Select(g => new { Browser = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => ValueTuple.Create(x.Browser, x.Count))
            .ToListAsync(ct);
    }

    public async Task<List<(string Os, int Count)>> GetOsBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.Os != null)
            .GroupBy(e => e.Os!)
            .Select(g => new { Os = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => ValueTuple.Create(x.Os, x.Count))
            .ToListAsync(ct);
    }

    public async Task<List<(string Referrer, int Count)>> GetReferrerBreakdownAsync(DateTime from, DateTime to, int count, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .GroupBy(e => e.Referrer ?? "(direct)")
            .Select(g => new { Referrer = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .Select(x => ValueTuple.Create(x.Referrer, x.Count))
            .ToListAsync(ct);
    }

    public async Task<List<(string Country, int Count)>> GetCountryBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to && e.Country != null)
            .GroupBy(e => e.Country!)
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => ValueTuple.Create(x.Country, x.Count))
            .ToListAsync(ct);
    }

    private static void AddDateParams(System.Data.Common.DbCommand cmd, DateTime from, DateTime to)
    {
        var startParam = cmd.CreateParameter();
        startParam.ParameterName = "@StartDate";
        startParam.Value = from;
        cmd.Parameters.Add(startParam);

        var endParam = cmd.CreateParameter();
        endParam.ParameterName = "@EndDate";
        endParam.Value = to;
        cmd.Parameters.Add(endParam);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
