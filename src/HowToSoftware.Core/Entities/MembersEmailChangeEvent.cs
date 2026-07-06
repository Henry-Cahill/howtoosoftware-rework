namespace HowToSoftware.Core.Entities;

public class MembersEmailChangeEvent
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string ToEmail { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
