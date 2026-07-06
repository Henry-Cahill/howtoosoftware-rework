namespace HowToSoftware.Core.Entities;

public class ApGhostApPostMapping
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string GhostUuid { get; set; } = null!;
    public string ApId { get; set; } = null!;
    public byte[]? ApIdHash { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
