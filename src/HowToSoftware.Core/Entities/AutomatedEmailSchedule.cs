namespace HowToSoftware.Core.Entities;

/// <summary>
/// Queued automated email send, scheduled in the future via
/// <see cref="AutomatedEmail.DelayMinutes"/>. A background service picks up
/// rows where <see cref="ScheduledFor"/> &lt;= now AND <see cref="ProcessedAt"/>
/// IS NULL and dispatches them through the normal automated email pipeline.
/// </summary>
public class AutomatedEmailSchedule
{
    public string Id { get; set; } = null!;
    public string AutomatedEmailId { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string MemberUuid { get; set; } = null!;
    public string MemberEmail { get; set; } = null!;
    public string? MemberName { get; set; }

    /// <summary>Site URL captured at enqueue time so the worker can render absolute links.</summary>
    public string SiteUrl { get; set; } = null!;

    /// <summary>UTC timestamp at which this send becomes eligible for dispatch.</summary>
    public DateTime ScheduledFor { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when the worker dispatched this row (success or terminal failure).</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Free-form failure reason if dispatch failed.</summary>
    public string? FailureReason { get; set; }

    // Navigation
    public AutomatedEmail AutomatedEmail { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
