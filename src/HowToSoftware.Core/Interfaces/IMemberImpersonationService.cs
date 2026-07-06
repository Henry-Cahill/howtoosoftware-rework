using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Issues and consumes one-time "view as member" impersonation tokens.
/// The admin clicks "View as member" → a short-lived token is created →
/// the admin's browser hits the public site's verify endpoint which signs
/// them in as that member (with an impersonated_by claim).
/// </summary>
public interface IMemberImpersonationService
{
    /// <summary>How long the issued token stays valid.</summary>
    static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(2);

    /// <summary>How long the resulting impersonation session cookie lasts.</summary>
    static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Generate a fresh, single-use impersonation token for the given member,
    /// initiated by the given admin user. Returns the raw token to embed in
    /// the verify URL; only its hash is persisted.
    /// </summary>
    Task<string> CreateTokenAsync(
        string memberId,
        string adminUserId,
        string? adminUserEmail,
        CancellationToken ct = default);

    /// <summary>
    /// Verify and consume a token. Returns the member to sign in plus the
    /// admin user id (for the impersonated_by claim + audit log), or null
    /// if the token is missing / expired / already used / refers to an
    /// unknown member.
    /// </summary>
    Task<ImpersonationVerifyResult?> VerifyAndConsumeAsync(
        string rawToken,
        CancellationToken ct = default);
}

public sealed record ImpersonationVerifyResult(Member Member, string AdminUserId, string? AdminUserEmail);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
