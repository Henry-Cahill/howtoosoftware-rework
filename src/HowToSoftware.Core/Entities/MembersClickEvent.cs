namespace HowToSoftware.Core.Entities;

public class MembersClickEvent
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string RedirectId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Member Member { get; set; } = null!;
    public Redirect Redirect { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
