namespace HowToSoftware.Core.Entities;

public class MembersPaidSubscriptionEvent
{
    public string Id { get; set; } = null!;
    public string? Type { get; set; }
    public string MemberId { get; set; } = null!;
    public string? SubscriptionId { get; set; }
    public string? FromPlan { get; set; }
    public string? ToPlan { get; set; }
    public string Currency { get; set; } = null!;
    public string Source { get; set; } = null!;
    public int MrrDelta { get; set; }
    public DateTime CreatedAt { get; set; }

    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
