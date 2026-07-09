using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/email/webhooks")]
public class EmailWebhookController(
    ISuppressionService suppressionService,
    IAutomatedEmailService automatedEmailService,
    IOptions<MailSettings> mailSettings,
    ILogger<EmailWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Mailgun webhook endpoint for bounce and spam complaint events.
    /// Mailgun POSTs JSON with HMAC-SHA256 signature for verification.
    /// </summary>
    [HttpPost("mailgun")]
    public async Task<IActionResult> HandleMailgunWebhook([FromBody] MailgunWebhookPayload payload, CancellationToken ct)
    {
        if (!VerifySignature(payload.Signature))
        {
            logger.LogWarning("Invalid Mailgun webhook signature rejected");
            return Unauthorized();
        }

        var eventType = payload.EventData?.Event;
        var recipient = payload.EventData?.Recipient;

        if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(recipient))
            return Ok(); // Acknowledge but do nothing for incomplete payloads

        // Extract the email ID from custom variables if present
        var emailId = payload.EventData?.UserVariables?.TryGetValue("email_id", out var eid) == true ? eid : null;

        // Suppression side-effects (existing behavior — newsletter bounces +
        // spam complaints go to the global suppression list).
        switch (eventType)
        {
            case "failed" when payload.EventData?.Severity == "permanent":
                await suppressionService.HandleBounceAsync(recipient, emailId, ct);
                break;

            case "complained":
                await suppressionService.HandleSpamComplaintAsync(recipient, emailId, ct);
                break;
        }

        // Forward delivery events to the automated-email tracker so the per-
        // automated-email statistics (sent/delivered/opened/clicked/bounced)
        // populate from Mailgun event data. The tracker is a no-op when the
        // recipient was not the target of a recent automated email send.
        await ForwardToAutomatedEmailTrackerAsync(eventType, recipient, payload.EventData, ct);

        if (eventType is not ("failed" or "complained" or "delivered" or "opened" or "clicked"))
        {
            logger.LogDebug("Ignoring Mailgun event type: {EventType}", LogSanitizer.SanitizeForLog(eventType));
        }

        return Ok();
    }

    private async Task ForwardToAutomatedEmailTrackerAsync(
        string eventType, string recipient, MailgunEventData? data, CancellationToken ct)
    {
        // Map Mailgun event names to our internal canonical names.
        var (canonical, reason) = eventType switch
        {
            "delivered" => ("delivered", (string?)null),
            "opened" => ("opened", null),
            "clicked" => ("clicked", null),
            "failed" when data?.Severity == "permanent" => ("bounced", data?.Reason ?? data?.DeliveryStatus?.Description),
            "failed" => ("failed", data?.Reason ?? data?.DeliveryStatus?.Description),
            _ => (null, null),
        };

        if (canonical is null)
            return;

        try
        {
            await automatedEmailService.RecordDeliveryEventAsync(
                recipient, canonical, DateTime.UtcNow, reason, ct);
        }
        catch (Exception ex)
        {
            // Never let webhook persistence failures bubble back to Mailgun —
            // they would trigger retries and could poison the queue.
            logger.LogWarning(ex,
                "Failed to record automated-email delivery event {EventType} for {EmailHash}",
                LogSanitizer.SanitizeForLog(eventType), LogSanitizer.MaskEmail(recipient));
        }
    }

    private bool VerifySignature(MailgunSignature? signature)
    {
        if (signature is null)
            return false;

        var apiKey = mailSettings.Value.MailgunApiKey;
        if (string.IsNullOrEmpty(apiKey))
            return false;

        var data = signature.Timestamp + signature.Token;
        var keyBytes = Encoding.UTF8.GetBytes(apiKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.Signature ?? string.Empty));
    }
}

/// <summary>
/// Models the Mailgun webhook JSON payload.
/// </summary>
public sealed class MailgunWebhookPayload
{
    [JsonPropertyName("signature")]
    public MailgunSignature? Signature { get; set; }

    [JsonPropertyName("event-data")]
    public MailgunEventData? EventData { get; set; }
}

public sealed class MailgunSignature
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public sealed class MailgunEventData
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("delivery-status")]
    public MailgunDeliveryStatus? DeliveryStatus { get; set; }

    [JsonPropertyName("user-variables")]
    public Dictionary<string, string>? UserVariables { get; set; }
}

public sealed class MailgunDeliveryStatus
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
