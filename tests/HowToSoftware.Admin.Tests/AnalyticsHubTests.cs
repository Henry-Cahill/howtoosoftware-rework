using HowToSoftware.Admin.Hubs;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Admin.Tests;

public class AnalyticsHubTests
{
    // ── DTO round-trip ──────────────────────────────────────────

    [Fact]
    public void PostEngagementRow_HoldsValues()
    {
        var row = new PostEngagementRow("My Post", "my-post", 100, 60, 40, 15);

        Assert.Equal("My Post", row.PostTitle);
        Assert.Equal("my-post", row.PostSlug);
        Assert.Equal(100, row.TotalViews);
        Assert.Equal(60, row.MemberViews);
        Assert.Equal(40, row.AnonymousViews);
        Assert.Equal(15, row.UniqueMemberViewers);
    }

    [Fact]
    public void MemberEngagementRow_HoldsValues()
    {
        var lastSeen = new DateTime(2026, 3, 10, 14, 30, 0);
        var row = new MemberEngagementRow("uuid-1", "Alice", "alice@test.com", "paid", 50, 5, lastSeen, 8);

        Assert.Equal("uuid-1", row.MemberUuid);
        Assert.Equal("Alice", row.MemberName);
        Assert.Equal("alice@test.com", row.MemberEmail);
        Assert.Equal("paid", row.MemberStatus);
        Assert.Equal(50, row.Pageviews);
        Assert.Equal(5, row.Sessions);
        Assert.Equal(lastSeen, row.LastSeenAt);
        Assert.Equal(8, row.PostsRead);
    }

    [Fact]
    public void MemberEngagementRow_NullableFields()
    {
        var row = new MemberEngagementRow("uuid-2", null, null, "free", 10, 1, null, 2);

        Assert.Null(row.MemberName);
        Assert.Null(row.MemberEmail);
        Assert.Null(row.LastSeenAt);
    }

    [Fact]
    public void MemberPostActivityRow_HoldsValues()
    {
        var lastViewed = new DateTime(2026, 3, 9, 10, 0, 0);
        var row = new MemberPostActivityRow("Deep Dive", "deep-dive", 3, lastViewed);

        Assert.Equal("Deep Dive", row.PostTitle);
        Assert.Equal("deep-dive", row.PostSlug);
        Assert.Equal(3, row.Views);
        Assert.Equal(lastViewed, row.LastViewed);
    }

    [Fact]
    public void PostEngagementRow_MemberPlusAnonymous_EqualsTotalViews()
    {
        var row = new PostEngagementRow("Test", "test", 200, 120, 80, 30);

        Assert.Equal(row.TotalViews, row.MemberViews + row.AnonymousViews);
    }

    [Fact]
    public void SearchTermRow_HoldsValues()
    {
        var row = new SearchTermRow("how to software", "Google", 42, 30);

        Assert.Equal("how to software", row.Term);
        Assert.Equal("Google", row.Engine);
        Assert.Equal(42, row.Visits);
        Assert.Equal(30, row.UniqueVisitors);
    }

    [Fact]
    public async Task FakeService_GetSearchTerms_ReturnsEmptyByDefault()
    {
        IAnalyticsDashboardService svc = new FakeAnalyticsDashboardService();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var result = await svc.GetSearchTermsAsync(from, to);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ── Service interface contract ──────────────────────────────

    [Fact]
    public async Task FakeService_GetPostEngagement_ReturnsData()
    {
        IAnalyticsDashboardService svc = new FakeAnalyticsDashboardService();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var result = await svc.GetPostEngagementAsync(from, to);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Fake Post", result[0].PostTitle);
    }

    [Fact]
    public async Task FakeService_GetMemberEngagement_ReturnsData()
    {
        IAnalyticsDashboardService svc = new FakeAnalyticsDashboardService();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var result = await svc.GetMemberEngagementAsync(from, to);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("fake-uuid", result[0].MemberUuid);
    }

    [Fact]
    public async Task FakeService_GetMemberPostActivity_ReturnsData()
    {
        IAnalyticsDashboardService svc = new FakeAnalyticsDashboardService();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var result = await svc.GetMemberPostActivityAsync("fake-uuid", from, to);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Fake Activity", result[0].PostTitle);
    }

    [Fact]
    public async Task FakeService_GetPageDetail_ReturnsCompositeData()
    {
        IAnalyticsDashboardService svc = new FakeAnalyticsDashboardService();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var result = await svc.GetPageDetailAsync("/fake-post/", from, to);

        Assert.NotNull(result);
        Assert.Equal("/fake-post/", result.PagePath);
        Assert.Equal("Fake Post", result.PostTitle);
        Assert.Equal(100, result.Pageviews);
        Assert.Equal(result.Pageviews, result.MemberPageviews + result.AnonymousPageviews);
        Assert.NotEmpty(result.TimeSeries);
        Assert.NotEmpty(result.Referrers);
        Assert.NotEmpty(result.Devices);
    }

    [Fact]
    public void PageDetailResult_HoldsValues()
    {
        var result = new PageDetailResult(
            "/some-page/",
            "post-1",
            "My Post",
            500,
            300,
            120,
            380,
            new List<TrafficTimeSeriesRow>(),
            new List<ReferrerRow>(),
            new List<DeviceRow>());

        Assert.Equal("/some-page/", result.PagePath);
        Assert.Equal("post-1", result.PostId);
        Assert.Equal("My Post", result.PostTitle);
        Assert.Equal(500, result.Pageviews);
        Assert.Equal(300, result.UniqueVisitors);
        Assert.Equal(120, result.MemberPageviews);
        Assert.Equal(380, result.AnonymousPageviews);
        Assert.Equal(result.Pageviews, result.MemberPageviews + result.AnonymousPageviews);
    }

    // ── Fake service ────────────────────────────────────────────

    private sealed class FakeAnalyticsDashboardService : IAnalyticsDashboardService
    {
        public Task<AnalyticsKpiSummary> GetKpiSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new AnalyticsKpiSummary(0, 0, 0, 0, 0));

        public Task<List<TopPageRow>> GetTopPagesAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<TopPageRow>());

        public Task<List<TopSourceRow>> GetTopSourcesAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<TopSourceRow>());

        public Task<List<DeviceRow>> GetDeviceBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<DeviceRow>());

        public Task<List<BrowserRow>> GetBrowserBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<BrowserRow>());

        public Task<List<OsRow>> GetOsBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<OsRow>());

        public Task<List<ReferrerRow>> GetReferrerBreakdownAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<ReferrerRow>());

        public Task<List<CountryRow>> GetCountryBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<CountryRow>());

        public Task<List<HourlyTrafficRow>> GetHourlyTrafficAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<HourlyTrafficRow>());

        public Task<List<TrafficTimeSeriesRow>> GetTrafficTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<TrafficTimeSeriesRow>());

        public Task<List<UtmCampaignRow>> GetUtmCampaignsAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<UtmCampaignRow>());

        public Task<List<SearchTermRow>> GetSearchTermsAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<SearchTermRow>());

        public Task<List<PostEngagementRow>> GetPostEngagementAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<PostEngagementRow>
            {
                new("Fake Post", "fake-post", 50, 30, 20, 10)
            });

        public Task<List<MemberEngagementRow>> GetMemberEngagementAsync(DateTime from, DateTime to, int top = 20, CancellationToken ct = default)
            => Task.FromResult(new List<MemberEngagementRow>
            {
                new("fake-uuid", "Fake User", "fake@test.com", "free", 25, 3, DateTime.UtcNow, 5)
            });

        public Task<List<MemberPostActivityRow>> GetMemberPostActivityAsync(string memberUuid, DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new List<MemberPostActivityRow>
            {
                new("Fake Activity", "fake-activity", 2, DateTime.UtcNow)
            });

        public Task<PageDetailResult> GetPageDetailAsync(string path, DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(new PageDetailResult(
                path,
                "post-1",
                "Fake Post",
                100,
                40,
                30,
                70,
                new List<TrafficTimeSeriesRow>
                {
                    new(from.Date, from.Date.ToString("MMM d"), 50, 20, 15),
                    new(from.Date.AddDays(1), from.Date.AddDays(1).ToString("MMM d"), 50, 20, 15)
                },
                new List<ReferrerRow>
                {
                    new("https://google.com/", 25, 20)
                },
                new List<DeviceRow>
                {
                    new("Desktop", 30, 75m),
                    new("Mobile", 10, 25m)
                }));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
