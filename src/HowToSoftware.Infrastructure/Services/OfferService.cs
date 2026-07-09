using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;
using Product = HowToSoftware.Core.Entities.Product;

namespace HowToSoftware.Infrastructure.Services;

public sealed class OfferService : IOfferService
{
    private readonly AppDbContext _db;
    private readonly StripeSettings _settings;
    private readonly ILogger<OfferService> _logger;

    public OfferService(
        AppDbContext db,
        IOptions<StripeSettings> settings,
        ILogger<OfferService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    // ================================================================
    // Queries
    // ================================================================

    public async Task<List<Offer>> GetOffersAsync(CancellationToken ct = default)
    {
        return await _db.Offers
            .Include(o => o.Product)
            .Include(o => o.Redemptions)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Offer?> GetOfferAsync(string offerId, CancellationToken ct = default)
    {
        return await _db.Offers
            .Include(o => o.Product)
            .Include(o => o.Redemptions)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);
    }

    public async Task<Offer?> GetOfferByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Offers
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Code == code && o.Active, ct);
    }

    // ================================================================
    // Create
    // ================================================================

    public async Task<Offer> CreateOfferAsync(CreateOfferRequest request, CancellationToken ct = default)
    {
        // Validate product exists
        var product = await _db.Products.FindAsync([request.ProductId], ct)
            ?? throw new InvalidOperationException($"Product {request.ProductId} not found");

        // Validate code uniqueness
        var codeExists = await _db.Offers.AnyAsync(o => o.Code == request.Code, ct);
        if (codeExists)
            throw new InvalidOperationException($"Offer code '{request.Code}' is already in use");

        // Create Stripe coupon
        var stripeCouponId = await CreateStripeCouponAsync(request, product, ct);

        var now = DateTime.UtcNow;
        var offer = new Offer
        {
            Id = ObjectIdGenerator.New(),
            Active = true,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToLowerInvariant(),
            ProductId = request.ProductId,
            StripeCouponId = stripeCouponId,
            Interval = request.Interval,
            Currency = request.Currency ?? product.Currency,
            DiscountType = request.DiscountType,
            DiscountAmount = request.DiscountAmount,
            Duration = request.Duration,
            DurationInMonths = request.Duration == "repeating" ? request.DurationInMonths : null,
            PortalTitle = request.PortalTitle,
            PortalDescription = request.PortalDescription,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Offers.Add(offer);
        await _db.SaveChangesAsync(ct);

        // Reload with navigation properties
        offer.Product = product;

        _logger.LogInformation(
            "Created offer {OfferId} '{Name}' (code={Code}) for product {ProductId}, Stripe coupon {CouponId}",
            offer.Id, offer.Name, offer.Code, offer.ProductId, stripeCouponId);

        return offer;
    }

    // ================================================================
    // Update
    // ================================================================

    public async Task UpdateOfferAsync(string offerId, UpdateOfferRequest request, CancellationToken ct = default)
    {
        var offer = await _db.Offers.FindAsync([offerId], ct)
            ?? throw new InvalidOperationException($"Offer {offerId} not found");

        if (request.Name is not null)
        {
            offer.Name = request.Name.Trim();

            // Update Stripe coupon name
            if (offer.StripeCouponId is not null)
            {
                var couponService = new CouponService();
                await couponService.UpdateAsync(offer.StripeCouponId,
                    new CouponUpdateOptions { Name = offer.Name },
                    cancellationToken: ct);
            }
        }

        if (request.PortalTitle is not null) offer.PortalTitle = request.PortalTitle;
        if (request.PortalDescription is not null) offer.PortalDescription = request.PortalDescription;

        offer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated offer {OfferId}", LogSanitizer.SanitizeForLog(offerId));
    }

    // ================================================================
    // Archive
    // ================================================================

    public async Task ArchiveOfferAsync(string offerId, CancellationToken ct = default)
    {
        var offer = await _db.Offers.FindAsync([offerId], ct)
            ?? throw new InvalidOperationException($"Offer {offerId} not found");

        offer.Active = false;
        offer.UpdatedAt = DateTime.UtcNow;

        // Delete Stripe coupon so it cannot be applied
        if (offer.StripeCouponId is not null)
        {
            try
            {
                var couponService = new CouponService();
                await couponService.DeleteAsync(offer.StripeCouponId, cancellationToken: ct);
                _logger.LogInformation("Deleted Stripe coupon {CouponId} for archived offer {OfferId}",
                    offer.StripeCouponId, LogSanitizer.SanitizeForLog(offerId));
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Failed to delete Stripe coupon {CouponId} for offer {OfferId}",
                    offer.StripeCouponId, LogSanitizer.SanitizeForLog(offerId));
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Archived offer {OfferId}", LogSanitizer.SanitizeForLog(offerId));
    }

    // ================================================================
    // Redemptions
    // ================================================================

    public async Task RecordRedemptionAsync(string offerId, string memberId, string subscriptionId, CancellationToken ct = default)
    {
        _db.OfferRedemptions.Add(new OfferRedemption
        {
            Id = ObjectIdGenerator.New(),
            OfferId = offerId,
            MemberId = memberId,
            SubscriptionId = subscriptionId,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Recorded redemption of offer {OfferId} by member {MemberId}", offerId, memberId);
    }

    public async Task<List<OfferRedemption>> GetRedemptionsAsync(string offerId, CancellationToken ct = default)
    {
        return await _db.OfferRedemptions
            .Include(r => r.Member)
            .Where(r => r.OfferId == offerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    // ================================================================
    // Private helpers
    // ================================================================

    private async Task<string?> CreateStripeCouponAsync(CreateOfferRequest request, Product product, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_settings.SecretKey))
        {
            _logger.LogWarning("Stripe not configured — skipping coupon creation for offer '{Name}'", request.Name);
            return null;
        }

        var couponOptions = new CouponCreateOptions
        {
            Name = request.Name.Trim(),
            Duration = request.Duration,
        };

        if (request.Duration == "repeating" && request.DurationInMonths.HasValue)
        {
            couponOptions.DurationInMonths = request.DurationInMonths.Value;
        }

        if (request.DiscountType == "percent")
        {
            couponOptions.PercentOff = request.DiscountAmount;
        }
        else // "fixed"
        {
            couponOptions.AmountOff = request.DiscountAmount;
            couponOptions.Currency = request.Currency ?? product.Currency ?? "usd";
        }

        var couponService = new CouponService();
        var coupon = await couponService.CreateAsync(couponOptions, cancellationToken: ct);

        _logger.LogInformation("Created Stripe coupon {CouponId} for offer '{Name}'", coupon.Id, request.Name);
        return coupon.Id;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
