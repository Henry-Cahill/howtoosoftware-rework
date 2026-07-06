namespace HowToSoftware.Core.Entities;

public class StripeProduct
{
    public string Id { get; set; } = null!;
    public string? ProductId { get; set; }
    public string StripeProductId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Product? Product { get; set; }
    public ICollection<StripePrice> Prices { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
