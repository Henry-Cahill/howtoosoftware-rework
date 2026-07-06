using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface INewsletterRepository
{
    Task<Newsletter?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Newsletter>> GetAllAsync(CancellationToken ct = default);
    Task<List<Newsletter>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Newsletter newsletter, CancellationToken ct = default);
    Task UpdateAsync(Newsletter newsletter, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<int> GetSubscriberCountAsync(string newsletterId, CancellationToken ct = default);

    /// <summary>
    /// Aggregates per-newsletter delivery analytics across the most recent <paramref name="sendsLimit"/>
    /// sends. Open / failed counts come from email_recipients.opened_at / failed_at; the unsubscribe
    /// count per send is the number of members_subscribe_events with Subscribed=false for this newsletter
    /// between this send's SubmittedAt and the next (later) send's SubmittedAt (or now for the latest send).
    /// </summary>
    Task<NewsletterAnalytics> GetAnalyticsAsync(string newsletterId, int sendsLimit = 10, CancellationToken ct = default);

    /// <summary>
    /// Daily subscribe / unsubscribe activity for a newsletter across [fromUtc, toUtc] (UTC dates,
    /// inclusive). Sources from members_subscribe_events filtered by NewsletterId. CumulativeSubscribers
    /// reflects the end-of-day active subscriber count (members subscribed to this newsletter with
    /// EmailDisabled=false), back-projected from the current live count by replaying events.
    /// </summary>
    Task<IReadOnlyList<NewsletterGrowthPoint>> GetGrowthAsync(string newsletterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}

public sealed record NewsletterAnalytics(
    int TotalSends,
    int TotalRecipients,
    int TotalDelivered,
    int TotalOpened,
    int TotalFailed,
    int TotalUnsubscribed,
    IReadOnlyList<NewsletterSendStat> RecentSends);

public sealed record NewsletterSendStat(
    string EmailId,
    string? PostId,
    string? PostTitle,
    DateTime SubmittedAt,
    int EmailCount,
    int DeliveredCount,
    int OpenedCount,
    int FailedCount,
    int UnsubscribedCount);

public sealed record NewsletterGrowthPoint(
    DateOnly Date,
    int Subscribed,
    int Unsubscribed,
    int CumulativeSubscribers);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
