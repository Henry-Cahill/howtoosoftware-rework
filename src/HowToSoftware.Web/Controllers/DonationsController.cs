using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/donations")]
public class DonationsController(
    IDonationService donationService,
    ILogger<DonationsController> logger) : ControllerBase
{
    /// <summary>
    /// GET /api/donations/settings — returns donation configuration (currency, suggested amount).
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var settings = await donationService.GetSettingsAsync(ct);
        return Ok(new
        {
            settings.Currency,
            SuggestedAmount = settings.SuggestedAmountInCents,
        });
    }

    /// <summary>
    /// POST /api/donations/checkout — creates a Stripe Checkout session for a one-time donation.
    /// </summary>
    [HttpPost("checkout")]
    [EnableRateLimiting("donations")]
    public async Task<IActionResult> Checkout(
        [FromBody] DonationCheckoutRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!Url.IsLocalUrl(request.SuccessUrl))
            return BadRequest(new { error = "SuccessUrl must be a relative URL on this site" });

        if (!Url.IsLocalUrl(request.CancelUrl))
            return BadRequest(new { error = "CancelUrl must be a relative URL on this site" });

        // Check for authenticated member
        var memberId = User.FindFirstValue("member_id");

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await donationService.CreateDonationCheckoutSessionAsync(
                new CreateDonationRequest
                {
                    Email = request.Email,
                    AmountInCents = request.AmountInCents,
                    Currency = request.Currency ?? "USD",
                    SuccessUrl = $"{baseUrl}{request.SuccessUrl}",
                    CancelUrl = $"{baseUrl}{request.CancelUrl}",
                    Name = request.Name,
                    MemberId = memberId,
                    DonationMessage = request.DonationMessage,
                },
                ct);

            return Ok(new { url = checkoutUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Donation checkout failed for {Email}", LogSanitizer.SanitizeForLog(request.Email));
            return StatusCode(500, new { error = "Failed to create donation checkout session" });
        }
    }
}

public record DonationCheckoutRequest
{
    public required string Email { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "Amount must be between 1 and 1,000,000 cents ($10,000).")]
    public required int AmountInCents { get; init; }

    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public string? Currency { get; init; }
    public string? Name { get; init; }
    public string? DonationMessage { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
