namespace HowToSoftware.Core.Entities;

public class ApSite
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Host { get; set; } = null!;
    public string WebhookSecret { get; set; } = null!;
    public bool GhostPro { get; set; } = true;

    // Navigation properties
    public ICollection<ApUser> Users { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
