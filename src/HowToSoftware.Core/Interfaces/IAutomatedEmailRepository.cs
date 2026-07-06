using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IAutomatedEmailRepository
{
    Task<AutomatedEmail?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<AutomatedEmail?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<AutomatedEmail>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(AutomatedEmail automatedEmail, CancellationToken ct = default);
    Task UpdateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);

    Task<bool> HasRecipientAsync(string automatedEmailId, string memberId, CancellationToken ct = default);
    Task AddRecipientAsync(AutomatedEmailRecipient recipient, CancellationToken ct = default);
    Task<int> GetRecipientCountAsync(string automatedEmailId, CancellationToken ct = default);

    /// <summary>
    /// Returns aggregate delivery statistics for a single automated email.
    /// </summary>
    Task<AutomatedEmailStatistics> GetStatisticsAsync(string automatedEmailId, CancellationToken ct = default);

    /// <summary>
    /// Returns a statistics dictionary keyed by automated email id for the
    /// supplied ids. Missing entries default to <see cref="AutomatedEmailStatistics.Empty"/>.
    /// </summary>
    Task<Dictionary<string, AutomatedEmailStatistics>> GetStatisticsBatchAsync(
        IEnumerable<string> automatedEmailIds, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of recipient rows for an automated email, newest-first.
    /// </summary>
    Task<PagedResult<AutomatedEmailRecipient>> GetRecipientsAsync(
        string automatedEmailId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Updates the most recent recipient row matching <paramref name="email"/> with
    /// a delivery event (delivered / opened / clicked / failed / bounced).
    /// Only the latest recipient row for that address is touched, and only if
    /// the corresponding column is null (events are idempotent). Returns the
    /// id of the updated row, or null if no matching recipient was found.
    /// </summary>
    Task<string?> MarkRecipientEventAsync(
        string email, string eventType, DateTime occurredAt, string? failureReason, CancellationToken ct = default);

    // ── Drip / sequence (AUTO.4) ──

    /// <summary>
    /// Returns the set of active automated emails that match the given trigger:
    /// either <c>Slug == trigger</c> or <c>TriggerEvent == trigger</c>. Used to
    /// fan a single trigger out to every email subscribed to it (the basis of
    /// multi-step drip sequences).
    /// </summary>
    Task<List<AutomatedEmail>> GetActiveByTriggerAsync(string trigger, CancellationToken ct = default);

    /// <summary>True if the member already has a recipient row OR an unprocessed
    /// schedule row for the given automated email. Used to dedup enqueue.</summary>
    Task<bool> HasSentOrScheduledAsync(string automatedEmailId, string memberId, CancellationToken ct = default);

    Task AddScheduleAsync(AutomatedEmailSchedule schedule, CancellationToken ct = default);

    /// <summary>
    /// Returns up to <paramref name="limit"/> queued sends whose
    /// <c>ScheduledFor &lt;= now</c> and which have not yet been processed,
    /// oldest-first. Used by the drip dispatcher background service.
    /// </summary>
    Task<List<AutomatedEmailSchedule>> GetDueSchedulesAsync(DateTime now, int limit, CancellationToken ct = default);

    Task<AutomatedEmailSchedule?> GetScheduleByIdAsync(string id, CancellationToken ct = default);

    Task MarkScheduleProcessedAsync(string id, DateTime processedAt, string? failureReason, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
