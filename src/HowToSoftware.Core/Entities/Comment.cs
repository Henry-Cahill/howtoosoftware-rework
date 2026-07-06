namespace HowToSoftware.Core.Entities;

public class Comment
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? MemberId { get; set; }
    public string? ParentId { get; set; }
    public string? InReplyToId { get; set; }
    public string Status { get; set; } = "published";
    public string? Html { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Post Post { get; set; } = null!;
    public Member? Member { get; set; }
    public Comment? Parent { get; set; }
    public Comment? InReplyTo { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
    public ICollection<CommentLike> Likes { get; set; } = [];
    public ICollection<CommentReport> Reports { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
