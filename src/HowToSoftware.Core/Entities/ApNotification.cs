namespace HowToSoftware.Core.Entities;

public class ApNotification
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public byte EventType { get; set; }
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public int? PostId { get; set; }
    public int? InReplyToPostId { get; set; }
    public bool Read { get; set; }

    // Navigation properties
    public ApUser User { get; set; } = null!;
    public ApAccount Account { get; set; } = null!;
    public ApPost? Post { get; set; }
    public ApPost? InReplyToPost { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
