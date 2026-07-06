namespace HowToSoftware.Core.Entities;

public class Member
{
    public string Id { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public string TransientId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Status { get; set; } = "free";
    public string? Name { get; set; }
    public string? Expertise { get; set; }
    public string? Note { get; set; }
    public string? Geolocation { get; set; }
    public string? AvatarImage { get; set; }
    public bool EnableCommentNotifications { get; set; } = true;
    public int EmailCount { get; set; }
    public int EmailOpenedCount { get; set; }
    public int? EmailOpenRate { get; set; }
    public bool EmailDisabled { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastCommentedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Commenting { get; set; }

    // Navigation properties
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<MembersProduct> MembersProducts { get; set; } = [];
    public ICollection<MembersLabel> MembersLabels { get; set; } = [];
    public ICollection<MembersNewsletter> MembersNewsletters { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
