using System.Security.Claims;
using HowToSoftware.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Controllers;

/// <summary>
/// Public-site endpoint that consumes a one-time impersonation token issued by
/// the admin and signs the admin's browser in as the target member.
/// </summary>
[ApiController]
[Route("api/member/impersonate")]
public sealed class MemberImpersonationController : ControllerBase
{
    private readonly IMemberImpersonationService _impersonation;
    private readonly IAdminAuditService _audit;
    private readonly ILogger<MemberImpersonationController> _logger;

    public MemberImpersonationController(
        IMemberImpersonationService impersonation,
        IAdminAuditService audit,
        ILogger<MemberImpersonationController> logger)
    {
        _impersonation = impersonation;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/member/impersonate/verify?token=…
    /// Consumes the token, creates a short non-persistent member session cookie
    /// stamped with the originating admin id, audits the action, and redirects
    /// to the public site homepage.
    /// </summary>
    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { ok = false, error = "Missing token" });

        var result = await _impersonation.VerifyAndConsumeAsync(token, ct);
        if (result is null)
            return Redirect("/?impersonate=failed");

        var member = result.Member;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, member.Id),
            new("member_uuid", member.Uuid),
            new(ClaimTypes.Email, member.Email),
            new("member_status", member.Status),
            new("impersonated_by", result.AdminUserId),
        };

        if (!string.IsNullOrEmpty(result.AdminUserEmail))
            claims.Add(new("impersonated_by_email", result.AdminUserEmail));

        if (!string.IsNullOrEmpty(member.Name))
            claims.Add(new(ClaimTypes.Name, member.Name));

        var identity = new ClaimsIdentity(claims, MemberAuthController.MemberCookieScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(MemberAuthController.MemberCookieScheme, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(IMemberImpersonationService.SessionLifetime),
        });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(ua)) ua = null;

        await _audit.LogAsync(new AdminAuditEntry
        {
            AdminUserId = result.AdminUserId,
            AdminUserEmail = result.AdminUserEmail,
            Action = "member.impersonate.start",
            TargetType = "member",
            TargetId = member.Id,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                memberEmail = member.Email,
                memberStatus = member.Status,
                sessionMinutes = (int)IMemberImpersonationService.SessionLifetime.TotalMinutes,
            }),
            IpAddress = ip,
            UserAgent = ua,
        }, ct);

        _logger.LogInformation(
            "Admin {AdminUserId} started impersonation session for member {MemberId}",
            result.AdminUserId, member.Id);

        return Redirect("/?impersonate=active");
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
