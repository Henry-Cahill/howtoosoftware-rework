using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class NewsletterRepository(AppDbContext db) : INewsletterRepository
{
    public async Task<Newsletter?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Newsletters.FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public async Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.Newsletters.FirstOrDefaultAsync(n => n.Slug == slug, ct);
    }

    public async Task<List<Newsletter>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Newsletters
            .OrderBy(n => n.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<List<Newsletter>> GetActiveAsync(CancellationToken ct = default)
    {
        return await db.Newsletters
            .Where(n => n.Status == "active")
            .OrderBy(n => n.SortOrder)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Newsletter newsletter, CancellationToken ct = default)
    {
        db.Newsletters.Add(newsletter);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Newsletter newsletter, CancellationToken ct = default)
    {
        db.Newsletters.Update(newsletter);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Newsletters.Where(n => n.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<int> GetSubscriberCountAsync(string newsletterId, CancellationToken ct = default)
    {
        return await db.MembersNewsletters
            .Where(mn => mn.NewsletterId == newsletterId && !mn.Member.EmailDisabled)
            .CountAsync(ct);
    }

    public async Task<NewsletterAnalytics> GetAnalyticsAsync(string newsletterId, int sendsLimit = 10, CancellationToken ct = default)
    {
        if (sendsLimit < 1) sendsLimit = 1;

        // Most recent N sends for this newsletter (any status — includes failed/in-flight).
        var sends = await db.Emails
            .Where(e => e.NewsletterId == newsletterId)
            .OrderByDescending(e => e.SubmittedAt)
            .Take(sendsLimit)
            .Select(e => new
            {
                e.Id,
                e.PostId,
                PostTitle = e.Post != null ? e.Post.Title : null,
                e.SubmittedAt,
                e.EmailCount,
                e.DeliveredCount,
                e.OpenedCount,
                e.FailedCount,
            })
            .ToListAsync(ct);

        if (sends.Count == 0)
        {
            return new NewsletterAnalytics(0, 0, 0, 0, 0, 0, []);
        }

        // Order oldest → newest so we can derive per-send unsubscribe windows.
        sends.Reverse();

        var firstSent = sends[0].SubmittedAt;
        // Pull unsubscribe events for this newsletter from the first send onward.
        var unsubEvents = await db.MembersSubscribeEvents
            .Where(s => s.NewsletterId == newsletterId
                        && !s.Subscribed
                        && s.CreatedAt >= firstSent)
            .Select(s => s.CreatedAt)
            .ToListAsync(ct);

        var stats = new List<NewsletterSendStat>(sends.Count);
        for (var i = 0; i < sends.Count; i++)
        {
            var s = sends[i];
            var windowStart = s.SubmittedAt;
            var windowEnd = i + 1 < sends.Count ? sends[i + 1].SubmittedAt : DateTime.MaxValue;
            var unsubCount = unsubEvents.Count(t => t >= windowStart && t < windowEnd);

            stats.Add(new NewsletterSendStat(
                EmailId: s.Id,
                PostId: s.PostId,
                PostTitle: s.PostTitle,
                SubmittedAt: s.SubmittedAt,
                EmailCount: s.EmailCount,
                DeliveredCount: s.DeliveredCount,
                OpenedCount: s.OpenedCount,
                FailedCount: s.FailedCount,
                UnsubscribedCount: unsubCount));
        }

        return new NewsletterAnalytics(
            TotalSends: stats.Count,
            TotalRecipients: stats.Sum(x => x.EmailCount),
            TotalDelivered: stats.Sum(x => x.DeliveredCount),
            TotalOpened: stats.Sum(x => x.OpenedCount),
            TotalFailed: stats.Sum(x => x.FailedCount),
            TotalUnsubscribed: stats.Sum(x => x.UnsubscribedCount),
            RecentSends: stats);
    }

    public async Task<IReadOnlyList<NewsletterGrowthPoint>> GetGrowthAsync(
        string newsletterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        // Normalize to UTC date boundaries.
        var fromDate = DateOnly.FromDateTime(fromUtc.ToUniversalTime());
        var toDate = DateOnly.FromDateTime(toUtc.ToUniversalTime());
        if (toDate < fromDate) return [];

        var rangeStart = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEndExclusive = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Pull all subscribe events for this newsletter from the start of the range onward.
        // Splitting into per-day subscribe / unsubscribe counts in-memory keeps the SQL trivial.
        var events = await db.MembersSubscribeEvents
            .Where(s => s.NewsletterId == newsletterId && s.CreatedAt >= rangeStart)
            .Select(s => new { s.CreatedAt, s.Subscribed })
            .ToListAsync(ct);

        // Group by UTC date.
        var inRange = new Dictionary<DateOnly, (int sub, int unsub)>();
        int deltaAfterRange = 0; // net change for events strictly after the range (used to back-project current count).
        foreach (var e in events)
        {
            var d = DateOnly.FromDateTime(DateTime.SpecifyKind(e.CreatedAt, DateTimeKind.Utc));
            if (d > toDate)
            {
                deltaAfterRange += e.Subscribed ? 1 : -1;
                continue;
            }
            (int sub, int unsub) bucket = inRange.TryGetValue(d, out var v) ? v : (0, 0);
            if (e.Subscribed) bucket.sub++;
            else bucket.unsub++;
            inRange[d] = bucket;
        }

        // Current active subscriber count (members subscribed, mailable).
        var currentCount = await db.MembersNewsletters
            .Where(mn => mn.NewsletterId == newsletterId && !mn.Member.EmailDisabled)
            .CountAsync(ct);

        // End-of-range cumulative = current - (net subscribed events after the range).
        var endOfRangeCumulative = currentCount - deltaAfterRange;

        // Walk forward from the start of the range to produce one point per day, with cumulative
        // back-computed from endOfRangeCumulative by summing daily deltas inside the range.
        var totalDays = (toDate.DayNumber - fromDate.DayNumber) + 1;
        var points = new List<NewsletterGrowthPoint>(totalDays);
        var rangeNetDelta = inRange.Values.Sum(v => v.sub - v.unsub);
        var startOfRangeCumulative = endOfRangeCumulative - rangeNetDelta;

        var running = startOfRangeCumulative;
        for (var i = 0; i < totalDays; i++)
        {
            var day = fromDate.AddDays(i);
            var (sub, unsub) = inRange.TryGetValue(day, out var v) ? v : (0, 0);
            running += sub - unsub;
            points.Add(new NewsletterGrowthPoint(day, sub, unsub, running));
        }

        return points;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
