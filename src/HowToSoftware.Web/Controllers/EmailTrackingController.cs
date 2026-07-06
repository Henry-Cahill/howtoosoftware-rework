using HowToSoftware.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/email")]
public class EmailTrackingController(INewsletterService newsletterService) : ControllerBase
{
    // 1x1 transparent GIF (43 bytes)
    private static readonly byte[] TransparentPixel =
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // GIF89a
        0x01, 0x00, 0x01, 0x00,             // 1x1
        0x80, 0x00, 0x00,                   // GCT flag, 1 color
        0xFF, 0xFF, 0xFF,                   // color 0: white
        0xFF, 0xFF, 0xFF,                   // color 1: white
        0x21, 0xF9, 0x04,                   // graphic control extension
        0x01, 0x00, 0x00, 0x00, 0x00,       // transparent index 0
        0x2C, 0x00, 0x00, 0x00, 0x00,       // image descriptor
        0x01, 0x00, 0x01, 0x00, 0x00,       // 1x1, no LCT
        0x02, 0x02, 0x44, 0x01, 0x00,       // LZW min code size + data
        0x3B                                // trailer
    ];

    /// <summary>
    /// Records an email open event and returns a 1x1 transparent GIF.
    /// Called when email clients load the tracking pixel image.
    /// </summary>
    [HttpGet("open/{recipientId}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TrackOpen(string recipientId, CancellationToken ct)
    {
        await newsletterService.RecordOpenAsync(recipientId, ct);

        return File(TransparentPixel, "image/gif");
    }

    /// <summary>
    /// Records an email link click event and redirects to the destination URL.
    /// Called when a recipient clicks a tracked link in a newsletter email.
    /// </summary>
    [HttpGet("click/{redirectId}/{memberId}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TrackClick(string redirectId, string memberId, CancellationToken ct)
    {
        try
        {
            var destinationUrl = await newsletterService.RecordClickAsync(redirectId, memberId, ct);
            return Redirect(destinationUrl);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Records positive (1) or negative (0) feedback from a member for a newsletter post.
    /// Called when a recipient clicks the 👍 or 👎 link in a newsletter email.
    /// </summary>
    [HttpGet("feedback/{emailId}/{memberId}/{score:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RecordFeedback(string emailId, string memberId, int score, CancellationToken ct)
    {
        if (score is not (0 or 1))
            return BadRequest();

        try
        {
            await newsletterService.RecordFeedbackAsync(emailId, memberId, score, ct);
            return Content(FeedbackHtml(score), "text/html");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private static string FeedbackHtml(int score) =>
        $$"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8"><title>Thank you</title>
        <style>body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;display:flex;justify-content:center;align-items:center;min-height:100vh;margin:0;background:#f4f4f4;color:#15212A}div{text-align:center;padding:2rem}h1{font-size:3rem;margin:0 0 1rem}</style>
        </head><body><div><h1>{{(score == 1 ? "\U0001f44d" : "\U0001f44e")}}</h1><p>Thanks for your feedback!</p></div></body></html>
        """;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
