namespace HowToSoftware.Core.Entities;

public class MembersProduct
{
    public string Id { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public int SortOrder { get; set; }
    public DateTime? ExpiryAt { get; set; }

    // Navigation properties
    public Member Member { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
