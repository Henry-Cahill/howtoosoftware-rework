using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IMentionService
{
    /// <summary>Gets all non-deleted mentions, most recent first.</summary>
    Task<List<Mention>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the count of non-deleted mentions awaiting admin review
    /// (Status == "pending"). Used to render a badge on the Mentions
    /// nav item so admins can see at-a-glance how many items need
    /// approval/rejection.
    /// </summary>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);

    /// <summary>Gets a single mention by ID.</summary>
    Task<Mention?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Gets verified mentions for a specific target URL.</summary>
    Task<List<Mention>> GetByTargetAsync(string targetUrl, CancellationToken ct = default);

    /// <summary>Gets verified mentions for a post by resource ID.</summary>
    Task<List<Mention>> GetByResourceAsync(string resourceId, string resourceType, CancellationToken ct = default);

    /// <summary>
    /// Receives a webmention: validates source/target, fetches the source page,
    /// extracts metadata, and stores the mention. Returns the created or updated mention.
    /// </summary>
    Task<Mention> ReceiveAsync(string source, string target, CancellationToken ct = default);

    /// <summary>Re-verifies a mention by fetching the source page again.</summary>
    Task VerifyAsync(string id, CancellationToken ct = default);

    /// <summary>Approves a mention so it renders on the public site.</summary>
    Task ApproveAsync(string id, CancellationToken ct = default);

    /// <summary>Rejects a mention so it never renders on the public site.</summary>
    Task RejectAsync(string id, CancellationToken ct = default);

    /// <summary>Soft-deletes a mention.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
