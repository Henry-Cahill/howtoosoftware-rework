using Microsoft.AspNetCore.Mvc;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Web.Controllers;

/// <summary>
/// Receives Webmention notifications per the W3C Webmention spec (https://www.w3.org/TR/webmention/).
/// POST /webmention with form-encoded source &amp; target parameters.
/// </summary>
[ApiController]
[Route("webmention")]
public class WebmentionController(
    IMentionService mentionService,
    ILogger<WebmentionController> logger) : ControllerBase
{
    /// <summary>
    /// POST /webmention — receive a webmention.
    /// Content-Type: application/x-www-form-urlencoded
    /// Parameters: source, target
    /// </summary>
    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Receive(
        [FromForm] string source,
        [FromForm] string target,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return BadRequest(new { error = "Both source and target parameters are required" });

        // Validate URL format before passing to service
        if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
            || (sourceUri.Scheme != "http" && sourceUri.Scheme != "https"))
            return BadRequest(new { error = "source must be an absolute HTTP or HTTPS URL" });

        if (!Uri.TryCreate(target, UriKind.Absolute, out var targetUri)
            || (targetUri.Scheme != "http" && targetUri.Scheme != "https"))
            return BadRequest(new { error = "target must be an absolute HTTP or HTTPS URL" });

        if (sourceUri == targetUri)
            return BadRequest(new { error = "source and target must be different URLs" });

        try
        {
            var mention = await mentionService.ReceiveAsync(source, target, ct);

            logger.LogInformation("Webmention received: {Source} → {Target} (verified={Verified})",
                LogSanitizer.SanitizeForLog(source), LogSanitizer.SanitizeForLog(target), mention.Verified);

            // Per the spec, return 202 Accepted for async processing
            return Accepted(new
            {
                id = mention.Id,
                source = mention.Source,
                target = mention.Target,
                verified = mention.Verified,
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /webmention?target={url} — retrieve verified mentions for a target URL.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetByTarget(
        [FromQuery] string? target,
        [FromQuery] string? resource_id,
        [FromQuery] string? resource_type,
        CancellationToken ct)
    {
        List<Core.Entities.Mention> mentions;

        if (!string.IsNullOrEmpty(resource_id) && !string.IsNullOrEmpty(resource_type))
        {
            mentions = await mentionService.GetByResourceAsync(resource_id, resource_type, ct);
        }
        else if (!string.IsNullOrEmpty(target))
        {
            mentions = await mentionService.GetByTargetAsync(target, ct);
        }
        else
        {
            return BadRequest(new { error = "Provide target URL or resource_id + resource_type" });
        }

        return Ok(new
        {
            mentions = mentions.Select(m => new
            {
                m.Id,
                m.Source,
                source_title = m.SourceTitle,
                source_site_title = m.SourceSiteTitle,
                source_excerpt = m.SourceExcerpt,
                source_author = m.SourceAuthor,
                source_featured_image = m.SourceFeaturedImage,
                source_favicon = m.SourceFavicon,
                m.Target,
                resource_id = m.ResourceId,
                resource_type = m.ResourceType,
                created_at = m.CreatedAt,
                m.Verified,
                status = m.Status,
            }),
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
