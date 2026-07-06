namespace HowToSoftware.Core.Entities;

public class PostRevision
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? Lexical { get; set; }
    public long CreatedAtTs { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AuthorId { get; set; }
    public string? Title { get; set; }
    public string? PostStatus { get; set; }
    public string? Reason { get; set; }
    public string? FeatureImage { get; set; }
    public string? FeatureImageAlt { get; set; }
    public string? FeatureImageCaption { get; set; }
    public string? CustomExcerpt { get; set; }

    // Navigation
    public User? Author { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
