using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/stripe/webhooks")]
public class StripeWebhookController(
    IStripeService stripeService,
    IDonationService donationService,
    IOptions<StripeSettings> stripeSettings,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);

        var webhookSecret = stripeSettings.Value.WebhookSecret;
        if (string.IsNullOrEmpty(webhookSecret))
        {
            logger.LogError("Stripe webhook secret not configured");
            return StatusCode(500);
        }

        Event stripeEvent;
        try
        {
            var signature = Request.Headers["Stripe-Signature"].ToString();
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature rejected");
            return BadRequest();
        }

        try
        {
            await stripeService.HandleWebhookEventAsync(stripeEvent.Type, json, ct);

            // Also route donation checkout completions to the donation service
            if (stripeEvent.Type == "checkout.session.completed")
                await donationService.RecordDonationAsync(json, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Stripe webhook event {EventType} ({EventId})",
                stripeEvent.Type, stripeEvent.Id);
        }

        return Ok();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
