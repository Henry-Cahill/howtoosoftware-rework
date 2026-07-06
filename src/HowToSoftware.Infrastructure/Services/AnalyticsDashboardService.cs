using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public class AnalyticsDashboardService(AppDbContext db, IAnalyticsRepository analyticsRepo) : IAnalyticsDashboardService
{
    public async Task<AnalyticsKpiSummary> GetKpiSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await analyticsRepo.GetKpiSummaryAsync(from, to, ct);
    }

    public async Task<List<TopPageRow>> GetTopPagesAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        // Aggregates pageviews + unique visitors per page_url_path, approximates avg
        // time-on-page using LEAD over each session (capped to 1..3600s to ignore
        // tail events on idle sessions), and LEFT JOINs the matched post by slug
        // (with or without trailing slash) to surface title / primary author / publish date.
        const string sql = """
            WITH PageStats AS (
                SELECT
                    ae.[page_url_path]              AS PagePath,
                    COUNT_BIG(*)                    AS Pageviews,
                    COUNT(DISTINCT ae.[session_id]) AS UniqueVisitors
                FROM [dbo].[analytics_events] ae
                WHERE ae.[timestamp] >= @StartDate
                  AND ae.[timestamp] <  @EndDate
                  AND ae.[page_url_path] IS NOT NULL
                GROUP BY ae.[page_url_path]
            ),
            TopStats AS (
                SELECT TOP (@Top) PagePath, Pageviews, UniqueVisitors
                FROM PageStats
                ORDER BY Pageviews DESC
            ),
            SessionLead AS (
                SELECT
                    ae.[page_url_path] AS PagePath,
                    DATEDIFF(SECOND, ae.[timestamp],
                        LEAD(ae.[timestamp]) OVER (PARTITION BY ae.[session_id] ORDER BY ae.[timestamp])
                    ) AS DurSec
                FROM [dbo].[analytics_events] ae
                WHERE ae.[timestamp] >= @StartDate
                  AND ae.[timestamp] <  @EndDate
                  AND ae.[session_id]    IS NOT NULL
                  AND ae.[page_url_path] IS NOT NULL
            ),
            AvgDur AS (
                SELECT PagePath, AVG(CAST(DurSec AS DECIMAL(18,2))) AS AvgTimeOnPageSeconds
                FROM SessionLead
                WHERE DurSec IS NOT NULL AND DurSec BETWEEN 1 AND 3600
                GROUP BY PagePath
            ),
            AuthorPick AS (
                SELECT pa.[post_id], pa.[author_id],
                       ROW_NUMBER() OVER (PARTITION BY pa.[post_id] ORDER BY pa.[sort_order], pa.[id]) AS rn
                FROM [dbo].[posts_authors] pa
            )
            SELECT
                ts.PagePath,
                CAST(ts.Pageviews AS INT)              AS Pageviews,
                ts.UniqueVisitors,
                p.[id]            AS PostId,
                p.[title]         AS PostTitle,
                p.[published_at]  AS PublishedAt,
                u.[name]          AS AuthorName,
                ad.AvgTimeOnPageSeconds
            FROM TopStats ts
            LEFT JOIN AvgDur ad ON ad.PagePath = ts.PagePath
            OUTER APPLY (
                SELECT TOP 1 p.[id], p.[title], p.[published_at]
                FROM [dbo].[posts] p
                WHERE p.[status] = 'published'
                  AND (
                        '/' + p.[slug] + '/' = ts.PagePath
                     OR '/' + p.[slug]       = ts.PagePath
                  )
                ORDER BY p.[published_at] DESC
            ) p
            LEFT JOIN AuthorPick ap ON ap.[post_id] = p.[id] AND ap.rn = 1
            LEFT JOIN [dbo].[users] u ON u.[id] = ap.[author_id]
            ORDER BY ts.Pageviews DESC;
            """;

        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            AddDateParams(cmd, from, to);
            var topParam = cmd.CreateParameter();
            topParam.ParameterName = "@Top";
            topParam.Value = top;
            cmd.Parameters.Add(topParam);

            var results = new List<TopPageRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            int iPath = reader.GetOrdinal("PagePath");
            int iViews = reader.GetOrdinal("Pageviews");
            int iVisitors = reader.GetOrdinal("UniqueVisitors");
            int iPostId = reader.GetOrdinal("PostId");
            int iTitle = reader.GetOrdinal("PostTitle");
            int iAuthor = reader.GetOrdinal("AuthorName");
            int iPublished = reader.GetOrdinal("PublishedAt");
            int iAvg = reader.GetOrdinal("AvgTimeOnPageSeconds");
            while (await reader.ReadAsync(ct))
            {
                results.Add(new TopPageRow(
                    reader.GetString(iPath),
                    reader.GetInt32(iViews),
                    reader.GetInt32(iVisitors),
                    reader.IsDBNull(iPostId) ? null : reader.GetString(iPostId),
                    reader.IsDBNull(iTitle) ? null : reader.GetString(iTitle),
                    reader.IsDBNull(iAuthor) ? null : reader.GetString(iAuthor),
                    reader.IsDBNull(iPublished) ? null : reader.GetDateTime(iPublished),
                    reader.IsDBNull(iAvg) ? null : reader.GetDecimal(iAvg)));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<TopSourceRow>> GetTopSourcesAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_TopSources @StartDate, @EndDate, @Top";
            AddDateParams(cmd, from, to);
            var topParam = cmd.CreateParameter();
            topParam.ParameterName = "@Top";
            topParam.Value = top;
            cmd.Parameters.Add(topParam);

            var results = new List<TopSourceRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new TopSourceRow(
                    reader.GetString(reader.GetOrdinal("Source")),
                    reader.GetInt32(reader.GetOrdinal("Visits")),
                    reader.GetInt32(reader.GetOrdinal("UniqueVisitors"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<DeviceRow>> GetDeviceBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_DeviceBreakdown @StartDate, @EndDate";
            AddDateParams(cmd, from, to);

            var results = new List<DeviceRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new DeviceRow(
                    reader.GetString(reader.GetOrdinal("Device")),
                    reader.GetInt32(reader.GetOrdinal("Sessions")),
                    reader.GetDecimal(reader.GetOrdinal("Percentage"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<CountryRow>> GetCountryBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_CountryBreakdown @StartDate, @EndDate";
            AddDateParams(cmd, from, to);

            var results = new List<CountryRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new CountryRow(
                    reader.GetString(reader.GetOrdinal("Country")),
                    reader.GetInt32(reader.GetOrdinal("Visitors")),
                    reader.GetDecimal(reader.GetOrdinal("Percentage"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<BrowserRow>> GetBrowserBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_BrowserBreakdown @StartDate, @EndDate";
            AddDateParams(cmd, from, to);

            var results = new List<BrowserRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new BrowserRow(
                    reader.GetString(reader.GetOrdinal("Browser")),
                    reader.GetInt32(reader.GetOrdinal("Sessions")),
                    reader.GetDecimal(reader.GetOrdinal("Percentage"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<OsRow>> GetOsBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_OsBreakdown @StartDate, @EndDate";
            AddDateParams(cmd, from, to);

            var results = new List<OsRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new OsRow(
                    reader.GetString(reader.GetOrdinal("Os")),
                    reader.GetInt32(reader.GetOrdinal("Sessions")),
                    reader.GetDecimal(reader.GetOrdinal("Percentage"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<ReferrerRow>> GetReferrerBreakdownAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_ReferrerBreakdown @StartDate, @EndDate, @Top";
            AddDateParams(cmd, from, to);
            var topParam = cmd.CreateParameter();
            topParam.ParameterName = "@Top";
            topParam.Value = top;
            cmd.Parameters.Add(topParam);

            var results = new List<ReferrerRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new ReferrerRow(
                    reader.GetString(reader.GetOrdinal("Referrer")),
                    reader.GetInt32(reader.GetOrdinal("Visits")),
                    reader.GetInt32(reader.GetOrdinal("UniqueVisitors"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<TrafficTimeSeriesRow>> GetTrafficTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var daySpan = (to - from).TotalDays;

        if (daySpan <= 7)
        {
            // Hourly granularity from rollup table
            var rows = await db.AnalyticsHourlyRollups
                .Where(r => r.BucketHour >= from && r.BucketHour < to)
                .OrderBy(r => r.BucketHour)
                .Select(r => new { r.BucketHour, r.Pageviews, r.UniqueVisitors })
                .ToListAsync(ct);

            // Member-only pageviews per hour bucket — read from raw events since the
            // rollup tables don't carry a member dimension.
            var memberByHour = await db.AnalyticsEvents
                .Where(e => e.Timestamp >= from
                            && e.Timestamp < to
                            && e.MemberUuid != null
                            && e.MemberUuid != "")
                .GroupBy(e => e.Timestamp.Date.AddHours(e.Timestamp.Hour))
                .Select(g => new { Bucket = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Bucket, x => x.Count, ct);

            return rows.Select(r => new TrafficTimeSeriesRow(
                r.BucketHour,
                r.BucketHour.ToString("MMM d HH:mm"),
                r.Pageviews,
                r.UniqueVisitors,
                memberByHour.TryGetValue(r.BucketHour, out var m) ? m : 0)).ToList();
        }
        else
        {
            // Daily granularity from rollup table
            var rows = await db.AnalyticsDailyRollups
                .Where(r => r.BucketDate >= from.Date && r.BucketDate < to.Date)
                .OrderBy(r => r.BucketDate)
                .Select(r => new { r.BucketDate, r.Pageviews, r.UniqueVisitors })
                .ToListAsync(ct);

            var memberByDay = await db.AnalyticsEvents
                .Where(e => e.Timestamp >= from.Date
                            && e.Timestamp < to.Date
                            && e.MemberUuid != null
                            && e.MemberUuid != "")
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => new { Bucket = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Bucket, x => x.Count, ct);

            return rows.Select(r => new TrafficTimeSeriesRow(
                r.BucketDate,
                r.BucketDate.ToString("MMM d"),
                r.Pageviews,
                r.UniqueVisitors,
                memberByDay.TryGetValue(r.BucketDate.Date, out var m) ? m : 0)).ToList();
        }
    }

    public async Task<List<UtmCampaignRow>> GetUtmCampaignsAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        var rows = await db.MembersCreatedEvents
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to
                && e.UtmCampaign != null)
            .GroupBy(e => new { e.UtmCampaign, e.UtmSource, e.UtmMedium })
            .Select(g => new
            {
                Campaign = g.Key.UtmCampaign!,
                Source = g.Key.UtmSource,
                Medium = g.Key.UtmMedium,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(ct);

        return rows.Select(r => new UtmCampaignRow(
            r.Campaign,
            r.Source ?? "(none)",
            r.Medium ?? "(none)",
            r.Count)).ToList();
    }

    public async Task<List<SearchTermRow>> GetSearchTermsAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        // Pull pageview events whose referrer host matches a known search engine.
        // We can't parse query strings inside SQL Server cheanly, so we filter
        // candidates server-side then aggregate in-memory. Volume is bounded by
        // the time window and the search-engine host filter.
        var raw = await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp < to)
            .Where(e => e.Referrer != null && e.Referrer != ""
                && (e.Referrer.Contains("google.")
                 || e.Referrer.Contains("bing.")
                 || e.Referrer.Contains("yahoo.")
                 || e.Referrer.Contains("duckduckgo.")
                 || e.Referrer.Contains("ecosia.")
                 || e.Referrer.Contains("startpage.")
                 || e.Referrer.Contains("yandex.")
                 || e.Referrer.Contains("baidu.")
                 || e.Referrer.Contains("brave.com")))
            .Select(e => new { e.Referrer, e.SessionId })
            .ToListAsync(ct);

        // Aggregate by (engine, normalized term)
        var groups = new Dictionary<(string Engine, string Term), (int Visits, HashSet<string> Sessions)>();
        foreach (var r in raw)
        {
            if (string.IsNullOrEmpty(r.Referrer)) continue;
            if (!Uri.TryCreate(r.Referrer, UriKind.Absolute, out var uri)) continue;

            var (engine, paramName) = ResolveSearchEngine(uri.Host);
            if (engine is null || paramName is null) continue;

            var query = uri.Query;
            if (string.IsNullOrEmpty(query)) continue;

            var term = ExtractQueryParam(query, paramName);
            if (string.IsNullOrWhiteSpace(term)) continue;

            term = term.Trim();
            if (term.Length > 200) term = term[..200];

            var key = (engine, term.ToLowerInvariant());
            if (!groups.TryGetValue(key, out var agg))
            {
                agg = (0, new HashSet<string>(StringComparer.Ordinal));
                groups[key] = agg;
            }
            agg.Visits++;
            if (!string.IsNullOrEmpty(r.SessionId)) agg.Sessions.Add(r.SessionId);
            groups[key] = agg;
        }

        return groups
            .Select(kv => new SearchTermRow(kv.Key.Term, kv.Key.Engine, kv.Value.Visits, kv.Value.Sessions.Count))
            .OrderByDescending(r => r.Visits)
            .ThenByDescending(r => r.UniqueVisitors)
            .Take(top)
            .ToList();
    }

    private static (string? Engine, string? Param) ResolveSearchEngine(string host)
    {
        if (string.IsNullOrEmpty(host)) return (null, null);
        var h = host.ToLowerInvariant();
        // Strip leading "www."
        if (h.StartsWith("www.")) h = h[4..];

        if (h == "google.com" || h.EndsWith(".google.com") || h.StartsWith("google.")) return ("Google", "q");
        if (h == "bing.com" || h.EndsWith(".bing.com")) return ("Bing", "q");
        if (h == "duckduckgo.com" || h.EndsWith(".duckduckgo.com")) return ("DuckDuckGo", "q");
        if (h == "yahoo.com" || h.EndsWith(".yahoo.com") || h.StartsWith("yahoo.") || h.StartsWith("search.yahoo.")) return ("Yahoo", "p");
        if (h.Contains("ecosia.")) return ("Ecosia", "q");
        if (h.Contains("startpage.")) return ("Startpage", "query");
        if (h.Contains("yandex.")) return ("Yandex", "text");
        if (h.Contains("baidu.")) return ("Baidu", "wd");
        if (h == "search.brave.com" || h == "brave.com") return ("Brave", "q");
        return (null, null);
    }

    private static string? ExtractQueryParam(string query, string name)
    {
        // query begins with '?' — strip it
        if (query.Length > 0 && query[0] == '?') query = query[1..];
        if (query.Length == 0) return null;
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var key = pair[..eq];
            if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) continue;
            var value = pair[(eq + 1)..];
            try { return Uri.UnescapeDataString(value.Replace('+', ' ')); }
            catch { return null; }
        }
        return null;
    }

    public async Task<List<PostEngagementRow>> GetPostEngagementAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_PostEngagement @StartDate, @EndDate, @Top";
            AddDateParams(cmd, from, to);
            var topParam = cmd.CreateParameter();
            topParam.ParameterName = "@Top";
            topParam.Value = top;
            cmd.Parameters.Add(topParam);

            var results = new List<PostEngagementRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new PostEngagementRow(
                    reader.GetString(reader.GetOrdinal("PostTitle")),
                    reader.GetString(reader.GetOrdinal("PostSlug")),
                    reader.GetInt32(reader.GetOrdinal("TotalViews")),
                    reader.GetInt32(reader.GetOrdinal("MemberViews")),
                    reader.GetInt32(reader.GetOrdinal("AnonymousViews")),
                    reader.GetInt32(reader.GetOrdinal("UniqueMemberViewers"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<MemberEngagementRow>> GetMemberEngagementAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC dbo.usp_MemberEngagement @StartDate, @EndDate, @Top";
            AddDateParams(cmd, from, to);
            var topParam = cmd.CreateParameter();
            topParam.ParameterName = "@Top";
            topParam.Value = top;
            cmd.Parameters.Add(topParam);

            var results = new List<MemberEngagementRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new MemberEngagementRow(
                    reader.GetString(reader.GetOrdinal("MemberUuid")),
                    reader.IsDBNull(reader.GetOrdinal("MemberName")) ? null : reader.GetString(reader.GetOrdinal("MemberName")),
                    reader.IsDBNull(reader.GetOrdinal("MemberEmail")) ? null : reader.GetString(reader.GetOrdinal("MemberEmail")),
                    reader.GetString(reader.GetOrdinal("MemberStatus")),
                    reader.GetInt32(reader.GetOrdinal("Pageviews")),
                    reader.GetInt32(reader.GetOrdinal("Sessions")),
                    reader.IsDBNull(reader.GetOrdinal("LastSeenAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastSeenAt")),
                    reader.GetInt32(reader.GetOrdinal("PostsRead"))));
            }
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<MemberPostActivityRow>> GetMemberPostActivityAsync(string memberUuid, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Join analytics_events with posts to get per-post activity for a specific member
        return await db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp < to
                && e.MemberUuid == memberUuid
                && e.PostUuid != null)
            .Join(db.Posts,
                e => e.PostUuid,
                p => p.Uuid,
                (e, p) => new { p.Title, p.Slug, e.Timestamp })
            .GroupBy(x => new { x.Title, x.Slug })
            .Select(g => new MemberPostActivityRow(
                g.Key.Title,
                g.Key.Slug,
                g.Count(),
                g.Max(x => x.Timestamp)))
            .OrderByDescending(x => x.Views)
            .ToListAsync(ct);
    }

    public async Task<PageDetailResult> GetPageDetailAsync(string path, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // KPI totals for this page
        var events = db.AnalyticsEvents
            .Where(e => e.Timestamp >= from && e.Timestamp < to && e.PageUrlPath == path);

        var pageviews = await events.CountAsync(ct);
        var uniqueVisitors = await events
            .Where(e => e.SessionId != null)
            .Select(e => e.SessionId)
            .Distinct()
            .CountAsync(ct);
        var memberPageviews = await events
            .Where(e => e.MemberUuid != null && e.MemberUuid != "")
            .CountAsync(ct);
        var anonymousPageviews = pageviews - memberPageviews;

        // Resolve the post (if any) by slug — strip leading/trailing slashes
        var slug = path.Trim('/');
        var post = string.IsNullOrEmpty(slug)
            ? null
            : await db.Posts
                .Where(p => p.Status == "published" && p.Slug == slug)
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new { p.Id, p.Title })
                .FirstOrDefaultAsync(ct);

        // Time series — hourly for ≤7-day windows, daily otherwise
        var daySpan = (to - from).TotalDays;
        List<TrafficTimeSeriesRow> series;
        if (daySpan <= 7)
        {
            var rows = await events
                .GroupBy(e => e.Timestamp.Date.AddHours(e.Timestamp.Hour))
                .Select(g => new
                {
                    Bucket = g.Key,
                    Pageviews = g.Count(),
                    UniqueVisitors = g.Where(x => x.SessionId != null).Select(x => x.SessionId).Distinct().Count(),
                    MemberPageviews = g.Count(x => x.MemberUuid != null && x.MemberUuid != "")
                })
                .OrderBy(x => x.Bucket)
                .ToListAsync(ct);
            series = rows.Select(r => new TrafficTimeSeriesRow(
                r.Bucket, r.Bucket.ToString("MMM d HH:mm"), r.Pageviews, r.UniqueVisitors, r.MemberPageviews)).ToList();
        }
        else
        {
            var rows = await events
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => new
                {
                    Bucket = g.Key,
                    Pageviews = g.Count(),
                    UniqueVisitors = g.Where(x => x.SessionId != null).Select(x => x.SessionId).Distinct().Count(),
                    MemberPageviews = g.Count(x => x.MemberUuid != null && x.MemberUuid != "")
                })
                .OrderBy(x => x.Bucket)
                .ToListAsync(ct);
            series = rows.Select(r => new TrafficTimeSeriesRow(
                r.Bucket, r.Bucket.ToString("MMM d"), r.Pageviews, r.UniqueVisitors, r.MemberPageviews)).ToList();
        }

        // Referrers
        var refRows = await events
            .Where(e => e.Referrer != null && e.Referrer != "")
            .GroupBy(e => e.Referrer!)
            .Select(g => new
            {
                Referrer = g.Key,
                Visits = g.Count(),
                UniqueVisitors = g.Where(x => x.SessionId != null).Select(x => x.SessionId).Distinct().Count()
            })
            .OrderByDescending(x => x.Visits)
            .Take(20)
            .ToListAsync(ct);
        var referrers = refRows.Select(r => new ReferrerRow(r.Referrer, r.Visits, r.UniqueVisitors)).ToList();

        // Devices
        var devRows = await events
            .Where(e => e.Device != null && e.Device != "")
            .GroupBy(e => e.Device!)
            .Select(g => new
            {
                Device = g.Key,
                Sessions = g.Where(x => x.SessionId != null).Select(x => x.SessionId).Distinct().Count()
            })
            .ToListAsync(ct);
        var totalSessions = devRows.Sum(d => d.Sessions);
        var devices = devRows
            .OrderByDescending(d => d.Sessions)
            .Select(d => new DeviceRow(
                d.Device,
                d.Sessions,
                totalSessions > 0 ? Math.Round((decimal)d.Sessions * 100m / totalSessions, 2) : 0m))
            .ToList();

        return new PageDetailResult(
            path,
            post?.Id,
            post?.Title,
            pageviews,
            uniqueVisitors,
            memberPageviews,
            anonymousPageviews,
            series,
            referrers,
            devices);
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
