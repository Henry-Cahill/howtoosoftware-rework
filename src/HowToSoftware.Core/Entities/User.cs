using Microsoft.AspNetCore.Identity;

namespace HowToSoftware.Core.Entities;

public class User : IdentityUser<string>
{
    // IdentityUser<string> provides: Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    // EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber,
    // PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount

    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    /// <summary>Ghost bcrypt password hash (legacy — migrated from Ghost CMS, cleared after first Identity login).</summary>
    public string? GhostPassword { get; set; }
    public string? ProfileImage { get; set; }
    public string? CoverImage { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
    public string? Threads { get; set; }
    public string? Bluesky { get; set; }
    public string? Mastodon { get; set; }
    public string? TikTok { get; set; }
    public string? YouTube { get; set; }
    public string? Instagram { get; set; }
    public string? LinkedIn { get; set; }
    public string? Accessibility { get; set; }
    public string Status { get; set; } = "active";
    public string? Locale { get; set; }
    public string Visibility { get; set; } = "public";
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Tour { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool CommentNotifications { get; set; } = true;
    public bool FreeMemberSignupNotification { get; set; } = true;
    public bool PaidSubscriptionStartedNotification { get; set; } = true;
    public bool PaidSubscriptionCanceledNotification { get; set; }
    public bool MentionNotifications { get; set; } = true;
    public bool RecommendationNotifications { get; set; } = true;
    public bool MilestoneNotifications { get; set; } = true;
    public bool DonationNotifications { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<PostsAuthor> PostsAuthors { get; set; } = [];
    public ICollection<RolesUser> RolesUsers { get; set; } = [];
    public ICollection<PermissionsUser> PermissionsUsers { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
