namespace HowToSoftware.Core.Entities;

public class AutomatedEmailRecipient
{
    public string Id { get; set; } = null!;
    public string AutomatedEmailId { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string MemberUuid { get; set; } = null!;
    public string MemberEmail { get; set; } = null!;
    public string? MemberName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Delivery tracking ──
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public string? FailureReason { get; set; }

    // Navigation properties
    public AutomatedEmail AutomatedEmail { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
