namespace HowToSoftware.Core.Entities;

public class ApPost
{
    public int Id { get; set; }
    public string Uuid { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte Type { get; set; }
    public byte Audience { get; set; }
    public int AuthorId { get; set; }
    public string? Title { get; set; }
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string Url { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public int LikeCount { get; set; }
    public int RepostCount { get; set; }
    public int ReplyCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public string ApId { get; set; } = null!;
    public byte[]? ApIdHash { get; set; }
    public int? InReplyToId { get; set; }
    public int? ThreadRootId { get; set; }
    public string? Attachments { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? Metadata { get; set; }
    public string? Summary { get; set; }

    // Navigation properties
    public ApAccount Author { get; set; } = null!;
    public ApPost? InReplyTo { get; set; }
    public ApPost? ThreadRoot { get; set; }
    public ICollection<ApPost> Replies { get; set; } = [];
    public ICollection<ApFeed> Feeds { get; set; } = [];
    public ICollection<ApLike> Likes { get; set; } = [];
    public ICollection<ApMention> Mentions { get; set; } = [];
    public ICollection<ApOutbox> Outboxes { get; set; } = [];
    public ICollection<ApRepost> Reposts { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
