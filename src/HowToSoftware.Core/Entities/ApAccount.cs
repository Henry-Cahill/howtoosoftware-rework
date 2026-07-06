namespace HowToSoftware.Core.Entities;

public class ApAccount
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Username { get; set; } = null!;
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? Url { get; set; }
    public string? CustomFields { get; set; }
    public string ApId { get; set; } = null!;
    public string ApInboxUrl { get; set; } = null!;
    public string? ApSharedInboxUrl { get; set; }
    public string? ApPublicKey { get; set; }
    public string? ApPrivateKey { get; set; }
    public string? ApOutboxUrl { get; set; }
    public string? ApFollowingUrl { get; set; }
    public string? ApFollowersUrl { get; set; }
    public string? ApLikedUrl { get; set; }
    public string? Uuid { get; set; }
    public byte[]? ApIdHash { get; set; }
    public string Domain { get; set; } = null!;
    public byte[]? DomainHash { get; set; }
    public byte[]? ApInboxUrlHash { get; set; }

    // Navigation properties
    public ICollection<ApUser> Users { get; set; } = [];
    public ICollection<ApFollow> Followers { get; set; } = [];
    public ICollection<ApFollow> Following { get; set; } = [];
    public ICollection<ApBlock> BlockedBy { get; set; } = [];
    public ICollection<ApBlock> Blocking { get; set; } = [];
    public ICollection<ApDomainBlock> DomainBlocks { get; set; } = [];
    public ApAccountDeliveryBackoff? DeliveryBackoff { get; set; }
    public ICollection<ApPost> Posts { get; set; } = [];
    public ICollection<ApLike> Likes { get; set; } = [];
    public ICollection<ApMention> Mentions { get; set; } = [];
    public ICollection<ApRepost> Reposts { get; set; } = [];
    public ICollection<ApOutbox> Outboxes { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
