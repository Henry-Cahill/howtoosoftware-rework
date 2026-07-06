using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Manages trigger-based automated emails (welcome, etc.).
/// </summary>
public interface IAutomatedEmailService
{
    /// <summary>
    /// Sends an automated email identified by <paramref name="slug"/> to the given member,
    /// unless the member has already received it or the email is inactive.
    /// </summary>
    /// <remarks>
    /// <paramref name="slug"/> is matched against both <see cref="AutomatedEmail.Slug"/> AND
    /// <see cref="AutomatedEmail.TriggerEvent"/>, so a single trigger fans out to every
    /// subscribed email. Emails with <see cref="AutomatedEmail.DelayMinutes"/> &gt; 0 are
    /// queued in <c>automated_email_schedules</c> for later dispatch by
    /// <see cref="IAutomatedEmailService.DispatchScheduledAsync"/>.
    /// </remarks>
    Task SendAsync(string slug, Member member, string siteUrl, CancellationToken ct = default);

    /// <summary>
    /// Executes a single queued schedule row (called by the drip background
    /// dispatcher). Looks up the underlying automated email + reconstructs a
    /// minimal Member from the captured snapshot, then runs the same send
    /// pipeline as <see cref="SendAsync"/> (suppression check, dedup,
    /// render, send, recipient persistence). Marks the schedule row as
    /// processed on completion or failure.
    /// </summary>
    Task DispatchScheduledAsync(string scheduleId, CancellationToken ct = default);

    // ── CRUD for admin panel ──

    Task<AutomatedEmail?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<AutomatedEmail?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<AutomatedEmail>> GetAllAsync(CancellationToken ct = default);
    Task<AutomatedEmail> CreateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default);
    Task UpdateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<int> GetRecipientCountAsync(string automatedEmailId, CancellationToken ct = default);

    /// <summary>
    /// Returns aggregate delivery statistics for a single automated email.
    /// </summary>
    Task<AutomatedEmailStatistics> GetStatisticsAsync(string automatedEmailId, CancellationToken ct = default);

    /// <summary>
    /// Returns statistics for every supplied id. Missing ids map to
    /// <see cref="AutomatedEmailStatistics.Empty"/>.
    /// </summary>
    Task<Dictionary<string, AutomatedEmailStatistics>> GetStatisticsBatchAsync(
        IEnumerable<string> automatedEmailIds, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of recipient rows for an automated email, newest-first.
    /// </summary>
    Task<PagedResult<AutomatedEmailRecipient>> GetRecipientsAsync(
        string automatedEmailId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Routes a Mailgun delivery event ("delivered" / "opened" / "clicked" /
    /// "failed" / "bounced") to the most recent automated email recipient for
    /// the given address. Returns the affected recipient id, or null if no
    /// matching automated email recipient exists.
    /// </summary>
    Task<string?> RecordDeliveryEventAsync(
        string email, string eventType, DateTime occurredAt, string? failureReason, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
