using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IMemberService
{
    /// <summary>
    /// Registers a new free member. Subscribes them to newsletters that have
    /// <c>SubscribeOnSignup</c> enabled, records created/status/subscribe events,
    /// and sends a welcome email.
    /// Returns the created member, or null if the email is already registered.
    /// </summary>
    Task<Member?> SignupAsync(MemberSignupRequest request, CancellationToken ct = default);
}

public record MemberSignupRequest
{
    public required string Email { get; init; }
    public string? Name { get; init; }
    /// <summary>Newsletter IDs the member explicitly opted into. If null, auto-subscribes to all SubscribeOnSignup newsletters.</summary>
    public List<string>? NewsletterIds { get; init; }
    public required string SiteUrl { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
