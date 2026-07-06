namespace HowToSoftware.Core.Entities;

public class ApFeed
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public byte? PostType { get; set; }
    public byte? Audience { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public int AuthorId { get; set; }
    public int? RepostedById { get; set; }
    public DateTime PublishedAt { get; set; }

    // Navigation properties
    public ApUser User { get; set; } = null!;
    public ApPost Post { get; set; } = null!;
    public ApAccount Author { get; set; } = null!;
    public ApAccount? RepostedBy { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
