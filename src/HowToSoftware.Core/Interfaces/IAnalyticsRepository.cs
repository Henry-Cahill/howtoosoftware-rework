using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IAnalyticsRepository
{
    Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken ct = default);
    Task AddEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default);
    Task<PagedResult<AnalyticsEvent>> GetEventsAsync(DateTime from, DateTime to, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetVisitorCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> GetVisitCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> GetPageviewCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<decimal> GetBounceRateAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<decimal> GetAvgSessionDurationAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<AnalyticsKpiSummary> GetKpiSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<(string PageUrl, int Views)>> GetTopPagesAsync(DateTime from, DateTime to, int count, CancellationToken ct = default);
    Task<List<(string Source, int Visits)>> GetTopSourcesAsync(DateTime from, DateTime to, int count, CancellationToken ct = default);
    Task<List<(string Device, int Count)>> GetDeviceBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<(string Browser, int Count)>> GetBrowserBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<(string Os, int Count)>> GetOsBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<(string Referrer, int Count)>> GetReferrerBreakdownAsync(DateTime from, DateTime to, int count, CancellationToken ct = default);
    Task<List<(string Country, int Count)>> GetCountryBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
