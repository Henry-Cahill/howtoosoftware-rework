using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Filters members by engagement signals derived from the denormalised
/// EmailCount / EmailOpenedCount / LastSeenAt columns on <see cref="Member"/>.
/// </summary>
public enum MemberEngagementFilter
{
    /// <summary>LastSeenAt within the last 30 days.</summary>
    ActiveLast30Days,
    /// <summary>Received at least one email but never opened any.</summary>
    NeverOpenedEmail,
    /// <summary>Opened more than 50% of the emails they received.</summary>
    OpenedOverHalf,
    /// <summary>No LastSeenAt timestamp, or older than 90 days.</summary>
    InactiveLast90Days,
}

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Member?> GetByUuidAsync(string uuid, CancellationToken ct = default);
    Task<PagedResult<Member>> GetAllAsync(string? status, string? search, string? labelId, int page, int pageSize, MemberEngagementFilter? engagement = null, CancellationToken ct = default);
    Task<List<Member>> GetAllForExportAsync(string? status, string? labelId, MemberEngagementFilter? engagement = null, CancellationToken ct = default);
    Task<List<Member>> GetByLabelAsync(string labelId, CancellationToken ct = default);
    Task<List<Member>> GetNewsletterSubscribersAsync(string newsletterId, CancellationToken ct = default);
    Task AddAsync(Member member, CancellationToken ct = default);
    Task UpdateAsync(Member member, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<int> GetCountAsync(string? status, CancellationToken ct = default);
    Task AddLabelToMemberAsync(string memberId, string labelId, CancellationToken ct = default);
    Task RemoveLabelFromMemberAsync(string memberId, string labelId, CancellationToken ct = default);
    Task UpdateNoteAsync(string memberId, string? note, CancellationToken ct = default);
    Task<bool> LinkStripeCustomerAsync(string memberId, string stripeCustomerId, string? name, string? email, CancellationToken ct = default);

    Task AddCreatedEventAsync(MembersCreatedEvent evt, CancellationToken ct = default);
    Task AddStatusEventAsync(MembersStatusEvent evt, CancellationToken ct = default);
    Task AddSubscribeEventAsync(MembersSubscribeEvent evt, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
