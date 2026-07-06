namespace HowToSoftware.Core.Entities;

public class Tag
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? FeatureImage { get; set; }
    public string? ParentId { get; set; }
    public string Visibility { get; set; } = "public";
    public string? OgImage { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? TwitterImage { get; set; }
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CodeinjectionHead { get; set; }
    public string? CodeinjectionFoot { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? AccentColor { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<PostsTag> PostsTags { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
