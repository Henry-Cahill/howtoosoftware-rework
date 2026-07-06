namespace HowToSoftware.Core.Entities;

public class MembersPaymentEvent
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public int Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Source { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
