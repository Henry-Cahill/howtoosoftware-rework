namespace HowToSoftware.Core.Entities;

public class Subscription
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string TierId { get; set; } = null!;
    public string? Cadence { get; set; }
    public string? Currency { get; set; }
    public int? Amount { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentSubscriptionUrl { get; set; }
    public string? PaymentUserUrl { get; set; }
    public string? OfferId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Member Member { get; set; } = null!;
    public Product Tier { get; set; } = null!;
    public Offer? Offer { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
