using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/member")]
public class MemberAuthController : ControllerBase
{
    public const string MemberCookieScheme = "MemberCookie";
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(10);
    private const int MaxAttempts = 5;

    private readonly IMagicLinkService _magicLink;
    private readonly IMemberService _memberService;
    private readonly IBruteForceService _bruteForce;

    public MemberAuthController(
        IMagicLinkService magicLink,
        IMemberService memberService,
        IBruteForceService bruteForce)
    {
        _magicLink = magicLink;
        _memberService = memberService;
        _bruteForce = bruteForce;
    }

    /// <summary>
    /// POST /api/member/magic-link  — request a magic-link sign-in email.
    /// </summary>
    [HttpPost("magic-link")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SendMagicLink(
        [FromBody] MagicLinkRequestDto dto, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (await _bruteForce.TrackAsync($"magic-link:{ip}", MaxAttempts, RateWindow, ct))
            return StatusCode(429, new { ok = false, error = "Too many requests. Please try again later." });

        var siteUrl = $"{Request.Scheme}://{Request.Host}";

        var sent = await _magicLink.SendMagicLinkAsync(new MagicLinkRequest
        {
            Email = dto.Email,
            RedirectUrl = dto.RedirectUrl,
            SiteUrl = siteUrl,
        }, ct);

        // Always return 200 to prevent email enumeration
        return Ok(new { ok = true });
    }

    /// <summary>
    /// GET /api/member/magic-link/verify?token=…  — verify token, create session cookie, redirect.
    /// </summary>
    [HttpGet("magic-link/verify")]
    public async Task<IActionResult> VerifyMagicLink(
        [FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { ok = false, error = "Missing token" });

        var member = await _magicLink.VerifyTokenAsync(token, ct);
        if (member is null)
            return Redirect("/?signin=failed");

        // Build claims for the member session
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, member.Id),
            new("member_uuid", member.Uuid),
            new(ClaimTypes.Email, member.Email),
            new("member_status", member.Status),
        };

        if (!string.IsNullOrEmpty(member.Name))
            claims.Add(new(ClaimTypes.Name, member.Name));

        var identity = new ClaimsIdentity(claims, MemberCookieScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(MemberCookieScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        });

        // Redirect to the originally requested page, or homepage
        var redirect = "/?signin=success";
        return Redirect(redirect);
    }

    /// <summary>
    /// POST /api/member/signup  — register a new free member.
    /// Sends a verification email; session is only created after the link is clicked.
    /// </summary>
    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Signup(
        [FromBody] MemberSignupDto dto, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (await _bruteForce.TrackAsync($"signup:{ip}", MaxAttempts, RateWindow, ct))
            return StatusCode(429, new { ok = false, error = "Too many requests. Please try again later." });

        var siteUrl = $"{Request.Scheme}://{Request.Host}";

        var member = await _memberService.SignupAsync(new MemberSignupRequest
        {
            Email = dto.Email,
            Name = dto.Name,
            NewsletterIds = dto.NewsletterIds,
            SiteUrl = siteUrl,
        }, ct);

        // Always return 200 to prevent email enumeration
        if (member is null)
            return Ok(new { ok = true });

        // Send verification email instead of auto-signing in
        await _magicLink.SendSignupVerificationAsync(member, siteUrl, ct);

        return Ok(new { ok = true });
    }

    /// <summary>
    /// GET /api/member/signup/verify?token=…  — verify signup token, create session cookie, redirect.
    /// </summary>
    [HttpGet("signup/verify")]
    public async Task<IActionResult> VerifySignup(
        [FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { ok = false, error = "Missing token" });

        var member = await _magicLink.VerifySignupTokenAsync(token, ct);
        if (member is null)
            return Redirect("/?signup=failed");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, member.Id),
            new("member_uuid", member.Uuid),
            new(ClaimTypes.Email, member.Email),
            new("member_status", member.Status),
        };

        if (!string.IsNullOrEmpty(member.Name))
            claims.Add(new(ClaimTypes.Name, member.Name));

        var identity = new ClaimsIdentity(claims, MemberCookieScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(MemberCookieScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        });

        return Redirect("/?signup=success");
    }

    /// <summary>
    /// POST /api/member/signout  — sign out the current member session.
    /// </summary>
    [HttpPost("signout")]
    public new async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync(MemberCookieScheme);
        return Ok(new { ok = true });
    }
}

public class MagicLinkRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(191)]
    public string Email { get; set; } = null!;

    [StringLength(2000)]
    public string? RedirectUrl { get; set; }
}

public class MemberSignupDto
{
    [Required]
    [EmailAddress]
    [StringLength(191)]
    public string Email { get; set; } = null!;

    [StringLength(191)]
    public string? Name { get; set; }

    public List<string>? NewsletterIds { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
