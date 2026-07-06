namespace HowToSoftware.Core.Entities;

public class Offer
{
    public string Id { get; set; } = null!;
    public bool Active { get; set; } = true;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? ProductId { get; set; }
    public string? StripeCouponId { get; set; }
    public string Interval { get; set; } = null!;
    public string? Currency { get; set; }
    public string DiscountType { get; set; } = null!;
    public int DiscountAmount { get; set; }
    public string Duration { get; set; } = null!;
    public int? DurationInMonths { get; set; }
    public string? PortalTitle { get; set; }
    public string? PortalDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RedemptionType { get; set; } = "signup";

    // Navigation properties
    public Product? Product { get; set; }
    public ICollection<OfferRedemption> Redemptions { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
