using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/offers")]
public class OffersController(
    IOfferService offerService,
    IStripeService stripeService,
    ILogger<OffersController> logger) : ControllerBase
{
    /// <summary>
    /// Looks up an offer by its public code.
    /// Returns the offer details without sensitive fields.
    /// </summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var offer = await offerService.GetOfferByCodeAsync(code, ct);
        if (offer is null)
            return NotFound(new { error = "Offer not found or inactive" });

        return Ok(new
        {
            offer.Id,
            offer.Name,
            offer.Code,
            offer.DiscountType,
            offer.DiscountAmount,
            offer.Duration,
            offer.DurationInMonths,
            offer.Interval,
            offer.Currency,
            offer.PortalTitle,
            offer.PortalDescription,
            Tier = offer.Product is not null ? new
            {
                offer.Product.Id,
                offer.Product.Name,
                offer.Product.Description,
                offer.Product.MonthlyPrice,
                offer.Product.YearlyPrice,
                offer.Product.Currency,
            } : null,
        });
    }

    /// <summary>
    /// Creates a Stripe Checkout session with the offer's discount applied.
    /// The member must be authenticated.
    /// </summary>
    [HttpPost("{code}/checkout")]
    public async Task<IActionResult> Checkout(
        string code,
        [FromBody] OfferCheckoutRequest request,
        CancellationToken ct)
    {
        var memberId = User.FindFirstValue("member_id");
        if (string.IsNullOrEmpty(memberId))
            return Unauthorized(new { error = "Sign in to use this offer" });

        var offer = await offerService.GetOfferByCodeAsync(code, ct);
        if (offer is null)
            return NotFound(new { error = "Offer not found or inactive" });

        if (string.IsNullOrEmpty(offer.ProductId))
            return BadRequest(new { error = "Offer is not tied to a tier" });

        try
        {
            var cadence = offer.Interval == "year" ? "yearly" : "monthly";
            var checkoutUrl = await stripeService.CreateCheckoutSessionAsync(
                memberId,
                offer.ProductId,
                request.Cadence ?? cadence,
                request.SuccessUrl,
                request.CancelUrl,
                offerId: offer.Id,
                ct: ct);

            return Ok(new { url = checkoutUrl });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Checkout failed for offer {Code} member {MemberId}",
                LogSanitizer.SanitizeForLog(code), LogSanitizer.SanitizeForLog(memberId));
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record OfferCheckoutRequest
{
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public string? Cadence { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
