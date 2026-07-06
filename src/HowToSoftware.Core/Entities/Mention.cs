namespace HowToSoftware.Core.Entities;

public class Mention
{
    public string Id { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? SourceTitle { get; set; }
    public string? SourceSiteTitle { get; set; }
    public string? SourceExcerpt { get; set; }
    public string? SourceAuthor { get; set; }
    public string? SourceFeaturedImage { get; set; }
    public string? SourceFavicon { get; set; }
    public string Target { get; set; } = null!;
    public string? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Payload { get; set; }
    public bool Deleted { get; set; }
    public bool Verified { get; set; }

    /// <summary>
    /// Approval workflow status for public rendering: "pending", "approved", or "rejected".
    /// Only "approved" mentions are returned by GetByTargetAsync / GetByResourceAsync.
    /// New mentions received via ReceiveAsync start as "pending" until an admin reviews them.
    /// </summary>
    public string Status { get; set; } = MentionStatus.Pending;
}

public static class MentionStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}


// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
