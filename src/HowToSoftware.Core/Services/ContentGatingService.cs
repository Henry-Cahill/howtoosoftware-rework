using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

public class ContentGatingService(IMemberRepository memberRepository) : IContentGatingService
{
    public async Task<ContentAccessLevel> CheckAccessAsync(Post post, string? memberId, CancellationToken ct = default)
    {
        // Public posts are always fully accessible
        if (post.Visibility == "public")
            return ContentAccessLevel.Full;

        // All non-public visibility levels require an authenticated member
        if (string.IsNullOrEmpty(memberId))
        {
            return post.Visibility switch
            {
                "members" => ContentAccessLevel.RequiresMember,
                "paid" => ContentAccessLevel.RequiresPaid,
                "tiers" => ContentAccessLevel.RequiresTier,
                _ => ContentAccessLevel.RequiresMember,
            };
        }

        var member = await memberRepository.GetByIdAsync(memberId, ct);
        if (member is null)
            return ContentAccessLevel.RequiresMember;

        // "members" — any authenticated member (free, paid, comped)
        if (post.Visibility == "members")
            return ContentAccessLevel.Full;

        // "paid" — member must have paid or comped status
        if (post.Visibility == "paid")
        {
            return member.Status is "paid" or "comped"
                ? ContentAccessLevel.Full
                : ContentAccessLevel.RequiresPaid;
        }

        // "tiers" — member must own at least one of the post's required products
        if (post.Visibility == "tiers")
        {
            var requiredProductIds = post.PostsProducts
                .Select(pp => pp.ProductId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (requiredProductIds.Count == 0)
                return ContentAccessLevel.Full;

            var hasTier = member.MembersProducts
                .Any(mp => requiredProductIds.Contains(mp.ProductId)
                    && (mp.ExpiryAt is null || mp.ExpiryAt > DateTime.UtcNow));

            return hasTier ? ContentAccessLevel.Full : ContentAccessLevel.RequiresTier;
        }

        return ContentAccessLevel.RequiresMember;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
