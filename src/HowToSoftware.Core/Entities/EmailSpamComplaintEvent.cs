namespace HowToSoftware.Core.Entities;

public class EmailSpamComplaintEvent
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string EmailId { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Email Email { get; set; } = null!;
    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
