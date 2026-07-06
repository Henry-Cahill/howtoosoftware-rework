namespace HowToSoftware.Core.Entities;

public class OfferRedemption
{
    public string Id { get; set; } = null!;
    public string OfferId { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string SubscriptionId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Offer Offer { get; set; } = null!;
    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
