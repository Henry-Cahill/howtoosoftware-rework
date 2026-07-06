namespace HowToSoftware.Core.Entities;

/// <summary>
/// Append-only admin note about a Member. Each entry captures who wrote it
/// and when, and is rendered as a conversation-style thread on the member
/// detail page (MEM.7).
/// </summary>
public class MemberNote
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;

    /// <summary>
    /// User.Id of the admin who authored the note. Null for legacy notes
    /// backfilled from the original single-field Member.Note column.
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// Denormalized author display name captured at write time so historical
    /// entries remain readable even if the author user is later renamed or
    /// removed.
    /// </summary>
    public string? AuthorName { get; set; }

    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Member? Member { get; set; }
    public User? Author { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
