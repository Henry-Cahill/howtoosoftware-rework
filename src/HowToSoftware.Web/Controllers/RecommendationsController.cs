using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController(
    IRecommendationService recommendationService,
    ILogger<RecommendationsController> logger) : ControllerBase
{
    /// <summary>
    /// GET /api/recommendations — returns all recommendations.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var recommendations = await recommendationService.GetAllAsync(ct);
        return Ok(new
        {
            recommendations = recommendations.Select(r => new
            {
                r.Id,
                r.Url,
                r.Title,
                r.Excerpt,
                r.FeaturedImage,
                r.Favicon,
                r.Description,
                r.OneClickSubscribe,
                r.CreatedAt,
                r.UpdatedAt,
            }),
        });
    }

    /// <summary>
    /// POST /api/recommendations/{id}/click — records a click event.
    /// </summary>
    [HttpPost("{id}/click")]
    [EnableRateLimiting("recommendations")]
    public async Task<IActionResult> RecordClick(string id, CancellationToken ct)
    {
        var recommendation = await recommendationService.GetByIdAsync(id, ct);
        if (recommendation is null)
            return NotFound(new { error = "Recommendation not found" });

        var memberId = User.FindFirstValue("member_id");

        try
        {
            await recommendationService.RecordClickEventAsync(id, memberId, ct);
            return Ok(new { message = "Click recorded" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record recommendation click for {RecommendationId}", LogSanitizer.SanitizeForLog(id));
            return StatusCode(500, new { error = "Failed to record click" });
        }
    }

    /// <summary>
    /// POST /api/recommendations/{id}/subscribe — records a subscribe event.
    /// </summary>
    [HttpPost("{id}/subscribe")]
    [EnableRateLimiting("recommendations")]
    public async Task<IActionResult> RecordSubscribe(string id, CancellationToken ct)
    {
        var recommendation = await recommendationService.GetByIdAsync(id, ct);
        if (recommendation is null)
            return NotFound(new { error = "Recommendation not found" });

        var memberId = User.FindFirstValue("member_id");

        try
        {
            await recommendationService.RecordSubscribeEventAsync(id, memberId, ct);
            return Ok(new { message = "Subscribe recorded" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record recommendation subscribe for {RecommendationId}", LogSanitizer.SanitizeForLog(id));
            return StatusCode(500, new { error = "Failed to record subscribe" });
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
