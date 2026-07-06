namespace HowToSoftware.Core.Entities;

public class Suppression
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? EmailId { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Email? EmailEntity { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
