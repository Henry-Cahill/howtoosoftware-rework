using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IMagicLinkService
{
    /// <summary>
    /// Generates a magic-link token for the given member email and sends
    /// a sign-in email containing the verification URL.
    /// Returns true if the email was sent (or silently succeeds for unknown emails).
    /// </summary>
    Task<bool> SendMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default);

    /// <summary>
    /// Verifies a magic-link token value. Returns the member if the token is
    /// valid, unused, and not expired; otherwise returns null.
    /// The token is consumed (marked used) on successful verification.
    /// </summary>
    Task<Member?> VerifyTokenAsync(string tokenValue, CancellationToken ct = default);

    /// <summary>
    /// Generates a signup verification token for a newly created member and sends
    /// a confirmation email. The member must click the link to activate their session.
    /// </summary>
    Task<bool> SendSignupVerificationAsync(Member member, string siteUrl, CancellationToken ct = default);

    /// <summary>
    /// Verifies a signup verification token. Returns the member if the token is
    /// valid, unused, not expired, and is a signup-verification type; otherwise returns null.
    /// The token is consumed (marked used) on successful verification.
    /// </summary>
    Task<Member?> VerifySignupTokenAsync(string tokenValue, CancellationToken ct = default);
}

public record MagicLinkRequest
{
    public required string Email { get; init; }
    public string? RedirectUrl { get; init; }
    public required string SiteUrl { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
