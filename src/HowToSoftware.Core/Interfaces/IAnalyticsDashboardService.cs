namespace HowToSoftware.Core.Interfaces;

public interface IAnalyticsDashboardService
{
    Task<AnalyticsKpiSummary> GetKpiSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<TopPageRow>> GetTopPagesAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);
    Task<List<TopSourceRow>> GetTopSourcesAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);
    Task<List<DeviceRow>> GetDeviceBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<BrowserRow>> GetBrowserBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<OsRow>> GetOsBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<ReferrerRow>> GetReferrerBreakdownAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);
    Task<List<CountryRow>> GetCountryBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<TrafficTimeSeriesRow>> GetTrafficTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<UtmCampaignRow>> GetUtmCampaignsAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);

    // Search engine query terms (ANAL.7)
    Task<List<SearchTermRow>> GetSearchTermsAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);

    // Member activity correlation
    Task<List<PostEngagementRow>> GetPostEngagementAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);
    Task<List<MemberEngagementRow>> GetMemberEngagementAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default);
    Task<List<MemberPostActivityRow>> GetMemberPostActivityAsync(string memberUuid, DateTime from, DateTime to, CancellationToken ct = default);

    // Per-page drill-down (ANAL.6)
    Task<PageDetailResult> GetPageDetailAsync(string path, DateTime from, DateTime to, CancellationToken ct = default);
}

public record AnalyticsKpiSummary(
    int Visitors,
    int Visits,
    int Pageviews,
    decimal BounceRatePercent,
    decimal AvgSessionDurationSeconds);

public record TopPageRow(
    string PagePath,
    int Pageviews,
    int UniqueVisitors,
    string? PostId = null,
    string? PostTitle = null,
    string? AuthorName = null,
    DateTime? PublishedAt = null,
    decimal? AvgTimeOnPageSeconds = null);
public record TopSourceRow(string Source, int Visits, int UniqueVisitors);
public record DeviceRow(string Device, int Sessions, decimal Percentage);
public record BrowserRow(string Browser, int Sessions, decimal Percentage);
public record OsRow(string Os, int Sessions, decimal Percentage);
public record ReferrerRow(string Referrer, int Visits, int UniqueVisitors);
public record CountryRow(string Country, int Visitors, decimal Percentage);
public record HourlyTrafficRow(int HourOfDay, int Pageviews, int UniqueVisitors);
public record TrafficTimeSeriesRow(DateTime Bucket, string Label, int Pageviews, int UniqueVisitors, int MemberPageviews = 0);
public record UtmCampaignRow(string Campaign, string Source, string Medium, int Count);
public record SearchTermRow(string Term, string Engine, int Visits, int UniqueVisitors);
public record PostEngagementRow(string PostTitle, string PostSlug, int TotalViews, int MemberViews, int AnonymousViews, int UniqueMemberViewers);
public record MemberEngagementRow(string MemberUuid, string? MemberName, string? MemberEmail, string MemberStatus, int Pageviews, int Sessions, DateTime? LastSeenAt, int PostsRead);
public record MemberPostActivityRow(string PostTitle, string PostSlug, int Views, DateTime LastViewed);

public record PageDetailResult(
    string PagePath,
    string? PostId,
    string? PostTitle,
    int Pageviews,
    int UniqueVisitors,
    int MemberPageviews,
    int AnonymousPageviews,
    List<TrafficTimeSeriesRow> TimeSeries,
    List<ReferrerRow> Referrers,
    List<DeviceRow> Devices);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
