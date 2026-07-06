namespace HowToSoftware.Core.Entities;

public class ApUser
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int AccountId { get; set; }
    public int SiteId { get; set; }

    // Navigation properties
    public ApAccount Account { get; set; } = null!;
    public ApSite Site { get; set; } = null!;
    public ICollection<ApFeed> Feeds { get; set; } = [];
    public ICollection<ApNotification> Notifications { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
