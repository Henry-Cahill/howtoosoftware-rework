namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Aggregates member-related events from multiple tables into a single
/// chronological activity timeline for the admin member detail page.
/// </summary>
public interface IMemberActivityService
{
    Task<IReadOnlyList<MemberActivityItem>> GetForMemberAsync(string memberId, int top = 100, CancellationToken ct = default);
}

/// <summary>
/// A single entry on a member's activity timeline.
/// </summary>
/// <param name="Timestamp">UTC time the event occurred.</param>
/// <param name="Type">
/// Stable machine type discriminator. One of:
/// signup, login, status_change, subscribed, unsubscribed,
/// email_sent, email_opened, email_clicked,
/// subscription_created, subscription_changed, subscription_cancelled,
/// payment, comment, feedback.
/// </param>
/// <param name="Title">Short headline for the event.</param>
/// <param name="Description">Optional secondary description.</param>
/// <param name="LinkText">Optional anchor text for a related resource (post/newsletter).</param>
/// <param name="LinkUrl">Optional admin URL for the related resource.</param>
public sealed record MemberActivityItem(
    DateTime Timestamp,
    string Type,
    string Title,
    string? Description = null,
    string? LinkText = null,
    string? LinkUrl = null);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
