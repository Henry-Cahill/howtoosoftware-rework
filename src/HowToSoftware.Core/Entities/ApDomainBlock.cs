namespace HowToSoftware.Core.Entities;

public class ApDomainBlock
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int BlockerId { get; set; }
    public string Domain { get; set; } = null!;
    public byte[]? DomainHash { get; set; }

    // Navigation properties
    public ApAccount Blocker { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
