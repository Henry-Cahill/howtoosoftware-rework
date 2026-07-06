namespace HowToSoftware.Core.Entities;

public class PostMeta
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? OgImage { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? TwitterImage { get; set; }
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? EmailSubject { get; set; }
    /// <summary>Optional alternate subject line for A/B testing the newsletter send.</summary>
    public string? EmailSubjectB { get; set; }
    public string? Frontmatter { get; set; }
    public string? FeatureImageAlt { get; set; }
    public string? FeatureImageCaption { get; set; }
    public bool EmailOnly { get; set; }

    // Navigation
    public Post Post { get; set; } = null!;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
