namespace HowToSoftware.Core.Entities;

public class Newsletter
{
    public string Id { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool FeedbackEnabled { get; set; }
    public string Slug { get; set; } = null!;
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public string SenderReplyTo { get; set; } = "newsletter";
    public string Status { get; set; } = "active";
    public string Visibility { get; set; } = "members";
    public bool SubscribeOnSignup { get; set; } = true;
    /// <summary>
    /// When true, the public site exposes a per-newsletter archive page at
    /// /newsletter/{slug}/archive/ listing posts published through this newsletter.
    /// </summary>
    public bool ArchiveEnabled { get; set; }
    public int SortOrder { get; set; }
    public string? HeaderImage { get; set; }
    public bool ShowHeaderIcon { get; set; } = true;
    public bool ShowHeaderTitle { get; set; } = true;
    public bool ShowExcerpt { get; set; }
    public string TitleFontCategory { get; set; } = "sans_serif";
    public string TitleAlignment { get; set; } = "center";
    public bool ShowFeatureImage { get; set; } = true;
    public string BodyFontCategory { get; set; } = "sans_serif";
    public string? FooterContent { get; set; }
    public bool ShowBadge { get; set; } = true;
    public bool ShowHeaderName { get; set; } = true;
    public bool ShowPostTitleSection { get; set; } = true;
    public bool ShowCommentCta { get; set; } = true;
    public bool ShowSubscriptionDetails { get; set; }
    public bool ShowLatestPosts { get; set; }
    public string BackgroundColor { get; set; } = "light";
    public string? PostTitleColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string ButtonCorners { get; set; } = "rounded";
    public string ButtonStyle { get; set; } = "fill";
    public string TitleFontWeight { get; set; } = "bold";
    public string LinkStyle { get; set; } = "underline";
    public string ImageCorners { get; set; } = "square";
    public string HeaderBackgroundColor { get; set; } = "transparent";
    public string? SectionTitleColor { get; set; }
    public string? DividerColor { get; set; }
    public string? ButtonColor { get; set; } = "accent";
    public string? LinkColor { get; set; } = "accent";

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Email> Emails { get; set; } = [];
    public ICollection<MembersNewsletter> MembersNewsletters { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
