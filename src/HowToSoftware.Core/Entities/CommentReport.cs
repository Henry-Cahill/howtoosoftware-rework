namespace HowToSoftware.Core.Entities;

public class CommentReport
{
    public string Id { get; set; } = null!;
    public string CommentId { get; set; } = null!;
    public string? MemberId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Comment Comment { get; set; } = null!;
    public Member? Member { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
