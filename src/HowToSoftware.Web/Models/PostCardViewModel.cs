namespace HowToSoftware.Web.Models;

public sealed class PostCardViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string? FeatureImage { get; init; }
    public string? FeatureImageAlt { get; init; }
    public string Visibility { get; init; } = "public";
    public bool Featured { get; init; }
    public string? PrimaryTagName { get; init; }
    public string? Excerpt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ReadingTimeMinutes { get; init; }
    public string CssClasses { get; init; } = "post-card";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
