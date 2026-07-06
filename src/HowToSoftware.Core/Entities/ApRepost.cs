namespace HowToSoftware.Core.Entities;

public class ApRepost
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int AccountId { get; set; }
    public int PostId { get; set; }

    // Navigation properties
    public ApAccount Account { get; set; } = null!;
    public ApPost Post { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
