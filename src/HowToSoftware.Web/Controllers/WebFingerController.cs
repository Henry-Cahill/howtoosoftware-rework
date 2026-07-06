using Microsoft.AspNetCore.Mvc;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Controllers;

[ApiController]
public class WebFingerController(IActivityPubService apService) : ControllerBase
{
    /// <summary>
    /// GET /.well-known/webfinger?resource=acct:username@domain
    /// </summary>
    [HttpGet("/.well-known/webfinger")]
    public async Task<IActionResult> WebFinger(
        [FromQuery] string resource,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(resource))
            return BadRequest(new { error = "resource parameter is required" });

        var result = await apService.HandleWebFingerAsync(resource, ct);
        if (result is null)
            return NotFound(new { error = "Resource not found" });

        return Ok(new
        {
            subject = result.Subject,
            links = result.Links.Select(l => new
            {
                rel = l.Rel,
                type = l.Type,
                href = l.Href
            })
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
