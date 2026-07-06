using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Append-only repository for the per-member admin notes thread (MEM.7).
/// </summary>
public interface IMemberNoteRepository
{
    /// <summary>
    /// Returns the full notes thread for a member, oldest-first so the UI
    /// can render it as a conversation.
    /// </summary>
    Task<List<MemberNote>> GetByMemberAsync(string memberId, CancellationToken ct = default);

    Task AddAsync(MemberNote note, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
