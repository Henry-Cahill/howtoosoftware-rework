namespace HowToSoftware.Core.Entities;

public class MembersSubscribeEvent
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public bool Subscribed { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? Source { get; set; }
    public string? NewsletterId { get; set; }

    public Member Member { get; set; } = null!;
    public Newsletter? Newsletter { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
