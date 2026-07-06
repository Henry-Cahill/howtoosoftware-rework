namespace HowToSoftware.Core.Entities;

public class MembersNewsletter
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string NewsletterId { get; set; } = null!;

    // Navigation properties
    public Member Member { get; set; } = null!;
    public Newsletter Newsletter { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
