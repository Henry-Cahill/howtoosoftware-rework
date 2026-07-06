namespace HowToSoftware.Core.Entities;

public class MobiledocRevision
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? Mobiledoc { get; set; }
    public long CreatedAtTs { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
