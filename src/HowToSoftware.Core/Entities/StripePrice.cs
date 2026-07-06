namespace HowToSoftware.Core.Entities;

public class StripePrice
{
    public string Id { get; set; } = null!;
    public string StripePriceId { get; set; } = null!;
    public string StripeProductId { get; set; } = null!;
    public bool Active { get; set; }
    public string? Nickname { get; set; }
    public string Currency { get; set; } = null!;
    public int Amount { get; set; }
    public string Type { get; set; } = "recurring";
    public string? Interval { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public StripeProduct StripeProductEntity { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
