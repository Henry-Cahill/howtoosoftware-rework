using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface INewsletterService
{
    /// <summary>
    /// Composes a newsletter email from a published post and sends it to all
    /// subscribed, non-suppressed members of the target newsletter. Creates
    /// Email, EmailBatch, and EmailRecipient records. Injects open-tracking
    /// pixel and wraps links for click tracking.
    /// </summary>
    Task<Email> SendPostAsNewsletterAsync(SendNewsletterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Processes a pending Email record created by <see cref="IContentService.SendAsEmailAsync"/>.
    /// Segments members, creates batches (500/batch), sends via email service, and
    /// updates delivery status on each recipient and the Email record.
    /// </summary>
    Task ProcessPendingEmailAsync(string emailId, string siteUrl, CancellationToken ct = default);

    /// <summary>
    /// For an Email currently in <c>AbTestPhase == "testing"</c>, picks the winning
    /// subject line based on open counts among the A/B test cohorts and sends the
    /// remaining "holdout" recipients with the winning subject. Sets
    /// <c>AbTestPhase = "completed"</c> and records <c>AbTestWinnerVariant</c>,
    /// <c>AbTestOpensA</c>, and <c>AbTestOpensB</c>. Safe to call multiple times — a
    /// no-op when the email is not in the testing phase.
    /// </summary>
    Task<Email> SendAbTestWinnerAsync(string emailId, string siteUrl, CancellationToken ct = default);

    /// <summary>
    /// Records an open event for a specific email recipient.
    /// </summary>
    Task RecordOpenAsync(string emailRecipientId, CancellationToken ct = default);

    /// <summary>
    /// Records a click event for a specific redirect and member. Returns
    /// the destination URL to redirect the caller to.
    /// </summary>
    Task<string> RecordClickAsync(string redirectId, string memberId, CancellationToken ct = default);

    /// <summary>
    /// Subscribes a member to a newsletter.
    /// </summary>
    Task SubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes a member from a newsletter.
    /// </summary>
    Task UnsubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default);

    /// <summary>
    /// Returns the newsletter IDs a member is subscribed to.
    /// </summary>
    Task<List<string>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default);

    /// <summary>
    /// Records positive (score=1) or negative (score=0) feedback from a member
    /// for a post associated with a sent email. Creates or updates the
    /// <see cref="Entities.MembersFeedback"/> record.
    /// </summary>
    Task RecordFeedbackAsync(string emailId, string memberId, int score, CancellationToken ct = default);
}

public record SendNewsletterRequest
{
    public required string PostId { get; init; }
    public required string NewsletterId { get; init; }
    public string RecipientFilter { get; init; } = "all";

    /// <summary>
    /// Base URL of the site, used to construct tracking pixel and click-tracking
    /// redirect URLs (e.g. "https://howtoosoftware.com").
    /// </summary>
    public required string SiteUrl { get; init; }

    /// <summary>
    /// Optional alternate subject line. When non-empty (or when
    /// <see cref="Entities.PostMeta.EmailSubjectB"/> is non-empty on the post),
    /// the send becomes an A/B test: a <see cref="AbSplitPercent"/>% slice of
    /// subscribers receives <see cref="Entities.PostMeta.EmailSubject"/> as
    /// subject (variant "a"), another <see cref="AbSplitPercent"/>% receives this
    /// alternate (variant "b"), and the remainder is held back for a winner send
    /// after <see cref="AbWaitMinutes"/>.
    /// </summary>
    public string? SubjectB { get; init; }

    /// <summary>Percentage of subscribers per variant cohort (1..50). Default 10.</summary>
    public int AbSplitPercent { get; init; } = 10;

    /// <summary>Minutes to wait between test-cohort send and winner send. Default 120.</summary>
    public int AbWaitMinutes { get; init; } = 120;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
