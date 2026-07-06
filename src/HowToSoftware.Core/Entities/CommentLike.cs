namespace HowToSoftware.Core.Entities;

public class CommentLike
{
    public string Id { get; set; } = null!;
    public string CommentId { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Comment Comment { get; set; } = null!;
    public Member Member { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
