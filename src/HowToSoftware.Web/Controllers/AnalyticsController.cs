using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/analytics")]
[EnableRateLimiting("analytics")]
public class AnalyticsController(
    IAnalyticsRepository analyticsRepository,
    IGeoIpService geoIpService) : ControllerBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// POST /api/analytics/event — ingest a single analytics event.
    /// Called by the tracking script (analytics.js) via sendBeacon/XHR.
    /// </summary>
    [HttpPost("event")]
    public async Task<IActionResult> IngestEvent(
        [FromBody] AnalyticsEventRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        // Validate URL lengths to prevent abuse
        if (request.PageUrl?.Length > 2000 || request.Referrer?.Length > 2000)
            return BadRequest();

        // Enrich: resolve country from client IP
        string? country = null;
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp is not null)
        {
            // Check for forwarded IP (when behind Caddy/reverse proxy)
            var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                var firstIp = forwarded.Split(',', StringSplitOptions.TrimEntries)[0];
                if (System.Net.IPAddress.TryParse(firstIp, out var parsedIp))
                    remoteIp = parsedIp;
            }

            country = geoIpService.LookupCountry(remoteIp);
        }

        // Serialize UTM params to Payload JSON if present
        string? payload = null;
        if (request.Utm is not null)
            payload = JsonSerializer.Serialize(request.Utm, s_jsonOptions);

        var analyticsEvent = new AnalyticsEvent
        {
            Timestamp = DateTime.UtcNow,
            SessionId = Sanitize(request.SessionId, 100),
            Action = Sanitize(request.Action, 50),
            PageUrl = Sanitize(request.PageUrl, 2000),
            PageUrlPath = Sanitize(request.PageUrlPath, 500),
            Referrer = Sanitize(request.Referrer, 2000),
            Device = Sanitize(request.Device, 50),
            Browser = Sanitize(request.Browser, 50),
            Os = Sanitize(request.Os, 50),
            Country = country,
            MemberUuid = Sanitize(request.MemberUuid, 50),
            Payload = payload
        };

        await analyticsRepository.AddEventAsync(analyticsEvent, ct);

        return NoContent();
    }

    /// <summary>
    /// Truncate to max length and trim whitespace. Returns null for empty strings.
    /// </summary>
    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        return value.Length > maxLength ? value[..maxLength] : value;
    }
}

public sealed class AnalyticsEventRequest
{
    [JsonPropertyName("session_id")]
    [Required]
    [StringLength(100)]
    public string? SessionId { get; set; }

    [JsonPropertyName("action")]
    [Required]
    [StringLength(50)]
    public string? Action { get; set; }

    [JsonPropertyName("page_url")]
    [Required]
    [StringLength(2000)]
    public string? PageUrl { get; set; }

    [JsonPropertyName("page_url_path")]
    [StringLength(500)]
    public string? PageUrlPath { get; set; }

    [JsonPropertyName("referrer")]
    [StringLength(2000)]
    public string? Referrer { get; set; }

    [JsonPropertyName("device")]
    [StringLength(50)]
    public string? Device { get; set; }

    [JsonPropertyName("browser")]
    [StringLength(50)]
    public string? Browser { get; set; }

    [JsonPropertyName("os")]
    [StringLength(50)]
    public string? Os { get; set; }

    [JsonPropertyName("member_uuid")]
    [StringLength(50)]
    public string? MemberUuid { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("utm")]
    public UtmParams? Utm { get; set; }
}

public sealed class UtmParams
{
    [JsonPropertyName("utm_source")]
    [StringLength(200)]
    public string? UtmSource { get; set; }

    [JsonPropertyName("utm_medium")]
    [StringLength(200)]
    public string? UtmMedium { get; set; }

    [JsonPropertyName("utm_campaign")]
    [StringLength(200)]
    public string? UtmCampaign { get; set; }

    [JsonPropertyName("utm_content")]
    [StringLength(200)]
    public string? UtmContent { get; set; }

    [JsonPropertyName("utm_term")]
    [StringLength(200)]
    public string? UtmTerm { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
