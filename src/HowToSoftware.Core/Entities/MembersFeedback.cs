namespace HowToSoftware.Core.Entities;

public class MembersFeedback
{
    public string Id { get; set; } = null!;
    public int Score { get; set; }
    public string MemberId { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;
    public Post Post { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
