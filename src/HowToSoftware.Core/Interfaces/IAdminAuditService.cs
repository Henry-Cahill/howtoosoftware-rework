namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Append-only security audit log for admin-staff actions
/// (e.g. impersonating a member). Used by MEM.6.
/// </summary>
public interface IAdminAuditService
{
    /// <summary>
    /// Record an admin action. Failures are logged but never thrown to the caller —
    /// audit recording must not block the underlying action.
    /// </summary>
    Task LogAsync(AdminAuditEntry entry, CancellationToken ct = default);
}

public sealed record AdminAuditEntry
{
    public required string AdminUserId { get; init; }
    public string? AdminUserEmail { get; init; }
    public required string Action { get; init; }
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public string? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
