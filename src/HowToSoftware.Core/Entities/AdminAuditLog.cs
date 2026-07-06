namespace HowToSoftware.Core.Entities;

/// <summary>
/// Append-only audit trail of security-sensitive actions taken by admin staff
/// (e.g. member impersonation). Rows are never updated or deleted.
/// </summary>
public class AdminAuditLog
{
    public string Id { get; set; } = null!;

    /// <summary>Users.Id of the admin who performed the action.</summary>
    public string AdminUserId { get; set; } = null!;

    /// <summary>Email of the admin at action time (denormalized for retention).</summary>
    public string? AdminUserEmail { get; set; }

    /// <summary>Action key, e.g. "member.impersonate.start".</summary>
    public string Action { get; set; } = null!;

    /// <summary>Target entity type, e.g. "member".</summary>
    public string? TargetType { get; set; }

    /// <summary>Target entity id, e.g. the impersonated Member.Id.</summary>
    public string? TargetId { get; set; }

    /// <summary>Optional JSON metadata blob.</summary>
    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
