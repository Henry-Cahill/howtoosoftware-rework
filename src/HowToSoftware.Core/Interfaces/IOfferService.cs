using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IOfferService
{
    /// <summary>Gets all offers with product and redemption count.</summary>
    Task<List<Offer>> GetOffersAsync(CancellationToken ct = default);

    /// <summary>Gets a single offer by ID with product and redemptions.</summary>
    Task<Offer?> GetOfferAsync(string offerId, CancellationToken ct = default);

    /// <summary>Looks up an active offer by its public code.</summary>
    Task<Offer?> GetOfferByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Creates a new offer locally and syncs a coupon to Stripe.</summary>
    Task<Offer> CreateOfferAsync(CreateOfferRequest request, CancellationToken ct = default);

    /// <summary>Updates an offer's portal display fields.</summary>
    Task UpdateOfferAsync(string offerId, UpdateOfferRequest request, CancellationToken ct = default);

    /// <summary>Deactivates an offer so it can no longer be redeemed.</summary>
    Task ArchiveOfferAsync(string offerId, CancellationToken ct = default);

    /// <summary>Records that a member redeemed an offer during checkout.</summary>
    Task RecordRedemptionAsync(string offerId, string memberId, string subscriptionId, CancellationToken ct = default);

    /// <summary>Gets redemption history for an offer.</summary>
    Task<List<OfferRedemption>> GetRedemptionsAsync(string offerId, CancellationToken ct = default);
}

public record CreateOfferRequest
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string ProductId { get; init; }
    public required string DiscountType { get; init; }   // "percent" or "fixed"
    public required int DiscountAmount { get; init; }      // percent (0–100) or fixed amount in cents
    public required string Interval { get; init; }         // "month" or "year"
    public required string Duration { get; init; }         // "once", "repeating", or "forever"
    public int? DurationInMonths { get; init; }            // required when Duration == "repeating"
    public string? Currency { get; init; }                 // required when DiscountType == "fixed"
    public string? PortalTitle { get; init; }
    public string? PortalDescription { get; init; }
}

public record UpdateOfferRequest
{
    public string? Name { get; init; }
    public string? PortalTitle { get; init; }
    public string? PortalDescription { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
