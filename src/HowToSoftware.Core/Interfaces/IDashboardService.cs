using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityFeedItem>> GetRecentActivityAsync(int count = 20, CancellationToken ct = default);
    Task<IReadOnlyList<SystemHealthEntry>> GetSystemHealthAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledContentItem>> GetScheduledContentAsync(int count = 10, CancellationToken ct = default);
}

public class DashboardData
{
    public int PublishedPostCount { get; init; }
    public int DraftPostCount { get; init; }
    public int ScheduledPostCount { get; init; }
    public int PageCount { get; init; }
    public int TotalMemberCount { get; init; }
    public int FreeMemberCount { get; init; }
    public int PaidMemberCount { get; init; }
    public int NewsletterCount { get; init; }
    public required IReadOnlyList<RecentPostItem> RecentPosts { get; init; }
    public required IReadOnlyList<RecentMemberItem> RecentMembers { get; init; }

    /// <summary>7-day analytics trend (pageviews, visitors, sessions) for sparklines.</summary>
    public required IReadOnlyList<DailyTrendPoint> AnalyticsTrend { get; init; }
    /// <summary>Percentage change in visitors vs prior 7-day period. Null when no prior data.</summary>
    public double? VisitorChangePercent { get; init; }
    /// <summary>Percentage change in pageviews vs prior 7-day period. Null when no prior data.</summary>
    public double? PageviewChangePercent { get; init; }
    /// <summary>Percentage change in sessions vs prior 7-day period. Null when no prior data.</summary>
    public double? SessionChangePercent { get; init; }

    /// <summary>Today's visitor count (from hourly rollups).</summary>
    public int TodayVisitors { get; init; }
    /// <summary>Today's pageview count (from hourly rollups).</summary>
    public int TodayPageviews { get; init; }
    /// <summary>Today's top page path by pageviews. Null when no data.</summary>
    public string? TodayTopPage { get; init; }
}

public class DailyTrendPoint
{
    public DateTime Date { get; init; }
    public int Pageviews { get; init; }
    public int Visitors { get; init; }
    public int Sessions { get; init; }
}

public class RecentPostItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class RecentMemberItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public enum ActivityType
{
    PostPublished,
    MemberSignup,
    CommentPosted,
    NewsletterSent,
    DonationReceived,
}

public class ActivityFeedItem
{
    public ActivityType Type { get; init; }
    public DateTime OccurredAt { get; init; }
    public required string Summary { get; init; }
    public string? LinkUrl { get; init; }
}

public class SystemHealthEntry
{
    public required string Name { get; init; }
    public HealthStatus Status { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
}

public class ScheduledContentItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public DateTime ScheduledAt { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
