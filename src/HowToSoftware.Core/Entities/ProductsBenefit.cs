namespace HowToSoftware.Core.Entities;

public class ProductsBenefit
{
    public string Id { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public string BenefitId { get; set; } = null!;
    public int SortOrder { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public Benefit Benefit { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
