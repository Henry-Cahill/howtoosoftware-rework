using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Admin.Hubs;

[Authorize(Roles = "Owner,Administrator")]
public class AnalyticsHub(IAnalyticsDashboardService analytics) : Hub
{
    public async Task RequestDashboard(DateTime from, DateTime to)
    {
        var kpi = await analytics.GetKpiSummaryAsync(from, to);
        var topPages = await analytics.GetTopPagesAsync(from, to);
        var topSources = await analytics.GetTopSourcesAsync(from, to);
        var devices = await analytics.GetDeviceBreakdownAsync(from, to);
        var browsers = await analytics.GetBrowserBreakdownAsync(from, to);
        var operatingSystems = await analytics.GetOsBreakdownAsync(from, to);
        var referrers = await analytics.GetReferrerBreakdownAsync(from, to);
        var countries = await analytics.GetCountryBreakdownAsync(from, to);
        var trafficTimeSeries = await analytics.GetTrafficTimeSeriesAsync(from, to);
        var utm = await analytics.GetUtmCampaignsAsync(from, to);
        var searchTerms = await analytics.GetSearchTermsAsync(from, to);
        var postEngagement = await analytics.GetPostEngagementAsync(from, to);
        var memberEngagement = await analytics.GetMemberEngagementAsync(from, to);

        await Clients.Caller.SendAsync("ReceiveDashboard", new
        {
            Kpi = kpi,
            TopPages = topPages,
            TopSources = topSources,
            Devices = devices,
            Browsers = browsers,
            OperatingSystems = operatingSystems,
            Referrers = referrers,
            Countries = countries,
            TrafficTimeSeries = trafficTimeSeries,
            UtmCampaigns = utm,
            SearchTerms = searchTerms,
            PostEngagement = postEngagement,
            MemberEngagement = memberEngagement
        });
    }

    public async Task RequestMemberPostActivity(string memberUuid, DateTime from, DateTime to)
    {
        var activity = await analytics.GetMemberPostActivityAsync(memberUuid, from, to);
        await Clients.Caller.SendAsync("ReceiveMemberPostActivity", activity);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
