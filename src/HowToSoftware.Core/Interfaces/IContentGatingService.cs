using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public enum ContentAccessLevel
{
    /// <summary>Full access — render entire post.</summary>
    Full,
    /// <summary>Gated — visitor must sign in as any member.</summary>
    RequiresMember,
    /// <summary>Gated — member must have a paid subscription.</summary>
    RequiresPaid,
    /// <summary>Gated — member must have a specific tier/product.</summary>
    RequiresTier,
}

public interface IContentGatingService
{
    /// <summary>
    /// Determines whether a member (or anonymous visitor) can view the full
    /// content of a post, based on the post's visibility setting and the
    /// member's status / product entitlements.
    /// </summary>
    /// <param name="post">The post to check (must have PostsProducts loaded for tier-gated posts).</param>
    /// <param name="memberId">The authenticated member's ID, or null for anonymous visitors.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ContentAccessLevel> CheckAccessAsync(Post post, string? memberId, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
