namespace HowToSoftware.Core.Entities;

public class Recommendation
{
    public string Id { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string? FeaturedImage { get; set; }
    public string? Favicon { get; set; }
    public string? Description { get; set; }
    public bool OneClickSubscribe { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
