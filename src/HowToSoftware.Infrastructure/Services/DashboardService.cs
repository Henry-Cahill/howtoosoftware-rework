using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public class DashboardService(AppDbContext db, HealthCheckService healthChecks) : IDashboardService
{
    public async Task<DashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        var publishedPosts = await db.Posts.CountAsync(p => p.Status == "published" && p.Type == "post", ct);
        var draftPosts = await db.Posts.CountAsync(p => p.Status == "draft" && p.Type == "post", ct);
        var scheduledPosts = await db.Posts.CountAsync(p => p.Status == "scheduled" && p.Type == "post", ct);
        var pages = await db.Posts.CountAsync(p => p.Type == "page", ct);

        var totalMembers = await db.Members.CountAsync(ct);
        var freeMembers = await db.Members.CountAsync(m => m.Status == "free", ct);
        var paidMembers = await db.Members.CountAsync(m => m.Status == "paid", ct);

        var newsletters = await db.Newsletters.CountAsync(ct);

        var recentPosts = await db.Posts
            .Where(p => p.Type == "post")
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPostItem
            {
                Id = p.Id,
                Title = p.Title,
                Status = p.Status,
                PublishedAt = p.PublishedAt,
                CreatedAt = p.CreatedAt,
            })
            .ToListAsync(ct);

        var recentMembers = await db.Members
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new RecentMemberItem
            {
                Id = m.Id,
                Name = m.Name ?? "",
                Email = m.Email,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
            })
            .ToListAsync(ct);

        // Analytics trend: last 14 days of daily rollups for sparklines + comparison
        var today = DateTime.UtcNow.Date;
        var fourteenDaysAgo = today.AddDays(-14);
        var rollups = await db.AnalyticsDailyRollups
            .Where(r => r.BucketDate >= fourteenDaysAgo && r.BucketDate < today)
            .OrderBy(r => r.BucketDate)
            .Select(r => new { r.BucketDate, r.Pageviews, r.UniqueVisitors, r.Sessions })
            .ToListAsync(ct);

        var sevenDaysAgo = today.AddDays(-7);
        var currentPeriod = rollups.Where(r => r.BucketDate >= sevenDaysAgo).ToList();
        var priorPeriod = rollups.Where(r => r.BucketDate < sevenDaysAgo).ToList();

        var trend = currentPeriod
            .Select(r => new DailyTrendPoint
            {
                Date = r.BucketDate,
                Pageviews = r.Pageviews,
                Visitors = r.UniqueVisitors,
                Sessions = r.Sessions,
            })
            .ToList();

        double? visitorChange = ComputeChangePercent(
            priorPeriod.Sum(r => r.UniqueVisitors),
            currentPeriod.Sum(r => r.UniqueVisitors));
        double? pageviewChange = ComputeChangePercent(
            priorPeriod.Sum(r => r.Pageviews),
            currentPeriod.Sum(r => r.Pageviews));
        double? sessionChange = ComputeChangePercent(
            priorPeriod.Sum(r => r.Sessions),
            currentPeriod.Sum(r => r.Sessions));

        // Today's quick-glance stats from hourly rollups
        var todayStart = today; // today is already DateTime.UtcNow.Date
        var tomorrow = today.AddDays(1);
        var todayHourly = await db.AnalyticsHourlyRollups
            .Where(r => r.BucketHour >= todayStart && r.BucketHour < tomorrow)
            .ToListAsync(ct);
        var todayVisitors = todayHourly.Sum(r => r.UniqueVisitors);
        var todayPageviews = todayHourly.Sum(r => r.Pageviews);

        // Top page today from raw events (hourly rollups don't store per-page data)
        var todayTopPage = await db.AnalyticsEvents
            .Where(e => e.Timestamp >= todayStart && e.Timestamp < tomorrow && e.PageUrlPath != null)
            .GroupBy(e => e.PageUrlPath)
            .Select(g => new { Page = g.Key, Views = g.Count() })
            .OrderByDescending(x => x.Views)
            .Select(x => x.Page)
            .FirstOrDefaultAsync(ct);

        return new DashboardData
        {
            PublishedPostCount = publishedPosts,
            DraftPostCount = draftPosts,
            ScheduledPostCount = scheduledPosts,
            PageCount = pages,
            TotalMemberCount = totalMembers,
            FreeMemberCount = freeMembers,
            PaidMemberCount = paidMembers,
            NewsletterCount = newsletters,
            RecentPosts = recentPosts,
            RecentMembers = recentMembers,
            AnalyticsTrend = trend,
            VisitorChangePercent = visitorChange,
            PageviewChangePercent = pageviewChange,
            SessionChangePercent = sessionChange,
            TodayVisitors = todayVisitors,
            TodayPageviews = todayPageviews,
            TodayTopPage = todayTopPage,
        };
    }

    private static double? ComputeChangePercent(int prior, int current)
    {
        if (prior == 0 && current == 0) return null;
        if (prior == 0) return 100.0;
        return Math.Round((current - prior) / (double)prior * 100, 1);
    }

    public async Task<IReadOnlyList<ActivityFeedItem>> GetRecentActivityAsync(int count = 20, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Published posts — project raw columns, format in memory
        var postRows = await db.Posts
            .Where(p => p.Type == "post" && p.Status == "published" && p.PublishedAt != null && p.PublishedAt >= cutoff)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .Select(p => new { p.Id, p.Title, PublishedAt = p.PublishedAt!.Value })
            .ToListAsync(ct);
        var posts = postRows.Select(p => new ActivityFeedItem
        {
            Type = ActivityType.PostPublished,
            OccurredAt = p.PublishedAt,
            Summary = $"Published \"{p.Title}\"",
            LinkUrl = $"posts/{p.Id}",
        });

        // New member signups
        var signupRows = await db.MembersCreatedEvents
            .Where(e => e.CreatedAt >= cutoff)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .Select(e => new { e.MemberId, e.CreatedAt, Name = e.Member.Name, Email = e.Member.Email })
            .ToListAsync(ct);
        var signups = signupRows.Select(e => new ActivityFeedItem
        {
            Type = ActivityType.MemberSignup,
            OccurredAt = e.CreatedAt,
            Summary = $"New member: {e.Name ?? e.Email}",
            LinkUrl = $"members/{e.MemberId}",
        });

        // Comments
        var commentRows = await db.Comments
            .Where(c => c.CreatedAt >= cutoff && c.Status == "published")
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .Select(c => new { c.PostId, c.CreatedAt, PostTitle = c.Post.Title, MemberName = c.Member!.Name, MemberEmail = c.Member.Email })
            .ToListAsync(ct);
        var comments = commentRows.Select(c => new ActivityFeedItem
        {
            Type = ActivityType.CommentPosted,
            OccurredAt = c.CreatedAt,
            Summary = $"Comment on \"{c.PostTitle}\" by {c.MemberName ?? c.MemberEmail ?? "anonymous"}",
            LinkUrl = $"posts/{c.PostId}",
        });

        // Newsletters sent
        var newsletterRows = await db.Emails
            .Where(e => e.Status == "submitted" && e.SubmittedAt >= cutoff)
            .OrderByDescending(e => e.SubmittedAt)
            .Take(count)
            .Select(e => new { e.SubmittedAt, e.Subject, PostTitle = e.Post.Title, e.EmailCount })
            .ToListAsync(ct);
        var newsletters = newsletterRows.Select(e => new ActivityFeedItem
        {
            Type = ActivityType.NewsletterSent,
            OccurredAt = e.SubmittedAt,
            Summary = $"Newsletter sent: \"{e.Subject ?? e.PostTitle}\" to {e.EmailCount} recipients",
            LinkUrl = "newsletters",
        });

        // Donations
        var donationRows = await db.DonationPaymentEvents
            .Where(d => d.CreatedAt >= cutoff)
            .OrderByDescending(d => d.CreatedAt)
            .Take(count)
            .Select(d => new { d.CreatedAt, d.Amount, d.Currency, d.Name, d.Email })
            .ToListAsync(ct);
        var donations = donationRows.Select(d => new ActivityFeedItem
        {
            Type = ActivityType.DonationReceived,
            OccurredAt = d.CreatedAt,
            Summary = $"Donation of {d.Amount / 100m:C} {d.Currency} from {d.Name ?? d.Email}",
            LinkUrl = "donations",
        });

        return posts
            .Concat(signups)
            .Concat(comments)
            .Concat(newsletters)
            .Concat(donations)
            .OrderByDescending(a => a.OccurredAt)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<ScheduledContentItem>> GetScheduledContentAsync(int count = 10, CancellationToken ct = default)
    {
        return await db.Posts
            .Where(p => p.Status == "scheduled" && p.PublishedAt != null)
            .OrderBy(p => p.PublishedAt)
            .Take(count)
            .Select(p => new ScheduledContentItem
            {
                Id = p.Id,
                Title = p.Title,
                Type = p.Type,
                ScheduledAt = p.PublishedAt!.Value,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SystemHealthEntry>> GetSystemHealthAsync(CancellationToken ct = default)
    {
        var report = await healthChecks.CheckHealthAsync(ct);

        return report.Entries
            .Select(e => new SystemHealthEntry
            {
                Name = e.Key,
                Status = e.Value.Status switch
                {
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => Core.Interfaces.HealthStatus.Healthy,
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => Core.Interfaces.HealthStatus.Degraded,
                    _ => Core.Interfaces.HealthStatus.Unhealthy,
                },
                Description = e.Value.Description,
                Data = e.Value.Data,
            })
            .OrderBy(e => e.Status)
            .ThenBy(e => e.Name)
            .ToList();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
