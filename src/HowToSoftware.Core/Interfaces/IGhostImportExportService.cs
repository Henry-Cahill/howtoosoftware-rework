using System.Text.Json.Serialization;

namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Import and export data in Ghost JSON format.
/// </summary>
public interface IGhostImportExportService
{
    Task<GhostImportResult> ImportAsync(GhostExportRoot export, string importerId, CancellationToken ct = default);
    Task<GhostExportRoot> ExportAsync(CancellationToken ct = default);
}

// ── Ghost JSON envelope ──────────────────────────────────────────────

public sealed class GhostExportRoot
{
    [JsonPropertyName("db")]
    public List<GhostDatabase> Db { get; set; } = [];
}

public sealed class GhostDatabase
{
    [JsonPropertyName("meta")]
    public GhostExportMeta Meta { get; set; } = new();

    [JsonPropertyName("data")]
    public GhostData Data { get; set; } = new();
}

public sealed class GhostExportMeta
{
    [JsonPropertyName("exported_on")]
    public long ExportedOn { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "5.0.0";
}

public sealed class GhostData
{
    [JsonPropertyName("posts")]
    public List<GhostPost> Posts { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<GhostTag> Tags { get; set; } = [];

    [JsonPropertyName("users")]
    public List<GhostUser> Users { get; set; } = [];

    [JsonPropertyName("posts_tags")]
    public List<GhostPostsTag> PostsTags { get; set; } = [];

    [JsonPropertyName("posts_authors")]
    public List<GhostPostsAuthor> PostsAuthors { get; set; } = [];

    [JsonPropertyName("posts_meta")]
    public List<GhostPostMeta> PostsMeta { get; set; } = [];
}

// ── Posts ─────────────────────────────────────────────────────────────

public sealed class GhostPost
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = null!;

    [JsonPropertyName("mobiledoc")]
    public string? Mobiledoc { get; set; }

    [JsonPropertyName("lexical")]
    public string? Lexical { get; set; }

    [JsonPropertyName("html")]
    public string? Html { get; set; }

    [JsonPropertyName("comment_id")]
    public string? CommentId { get; set; }

    [JsonPropertyName("plaintext")]
    public string? Plaintext { get; set; }

    [JsonPropertyName("feature_image")]
    public string? FeatureImage { get; set; }

    [JsonPropertyName("featured")]
    public int Featured { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "post";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "public";

    [JsonPropertyName("email_recipient_filter")]
    public string EmailRecipientFilter { get; set; } = "all";

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("published_by")]
    public string? PublishedBy { get; set; }

    [JsonPropertyName("custom_excerpt")]
    public string? CustomExcerpt { get; set; }

    [JsonPropertyName("codeinjection_head")]
    public string? CodeinjectionHead { get; set; }

    [JsonPropertyName("codeinjection_foot")]
    public string? CodeinjectionFoot { get; set; }

    [JsonPropertyName("custom_template")]
    public string? CustomTemplate { get; set; }

    [JsonPropertyName("canonical_url")]
    public string? CanonicalUrl { get; set; }

    [JsonPropertyName("newsletter_id")]
    public string? NewsletterId { get; set; }

    [JsonPropertyName("show_title_and_feature_image")]
    public int ShowTitleAndFeatureImage { get; set; } = 1;
}

// ── Tags ─────────────────────────────────────────────────────────────

public sealed class GhostTag
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("feature_image")]
    public string? FeatureImage { get; set; }

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "public";

    [JsonPropertyName("og_image")]
    public string? OgImage { get; set; }

    [JsonPropertyName("og_title")]
    public string? OgTitle { get; set; }

    [JsonPropertyName("og_description")]
    public string? OgDescription { get; set; }

    [JsonPropertyName("twitter_image")]
    public string? TwitterImage { get; set; }

    [JsonPropertyName("twitter_title")]
    public string? TwitterTitle { get; set; }

    [JsonPropertyName("twitter_description")]
    public string? TwitterDescription { get; set; }

    [JsonPropertyName("meta_title")]
    public string? MetaTitle { get; set; }

    [JsonPropertyName("meta_description")]
    public string? MetaDescription { get; set; }

    [JsonPropertyName("codeinjection_head")]
    public string? CodeinjectionHead { get; set; }

    [JsonPropertyName("codeinjection_foot")]
    public string? CodeinjectionFoot { get; set; }

    [JsonPropertyName("canonical_url")]
    public string? CanonicalUrl { get; set; }

    [JsonPropertyName("accent_color")]
    public string? AccentColor { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

// ── Users (authors) ──────────────────────────────────────────────────

public sealed class GhostUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = null!;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("profile_image")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("cover_image")]
    public string? CoverImage { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("facebook")]
    public string? Facebook { get; set; }

    [JsonPropertyName("twitter")]
    public string? Twitter { get; set; }

    [JsonPropertyName("accessibility")]
    public string? Accessibility { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "public";

    [JsonPropertyName("meta_title")]
    public string? MetaTitle { get; set; }

    [JsonPropertyName("meta_description")]
    public string? MetaDescription { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

// ── Junction tables ──────────────────────────────────────────────────

public sealed class GhostPostsTag
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("post_id")]
    public string PostId { get; set; } = null!;

    [JsonPropertyName("tag_id")]
    public string TagId { get; set; } = null!;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}

public sealed class GhostPostsAuthor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("post_id")]
    public string PostId { get; set; } = null!;

    [JsonPropertyName("author_id")]
    public string AuthorId { get; set; } = null!;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}

public sealed class GhostPostMeta
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("post_id")]
    public string PostId { get; set; } = null!;

    [JsonPropertyName("og_image")]
    public string? OgImage { get; set; }

    [JsonPropertyName("og_title")]
    public string? OgTitle { get; set; }

    [JsonPropertyName("og_description")]
    public string? OgDescription { get; set; }

    [JsonPropertyName("twitter_image")]
    public string? TwitterImage { get; set; }

    [JsonPropertyName("twitter_title")]
    public string? TwitterTitle { get; set; }

    [JsonPropertyName("twitter_description")]
    public string? TwitterDescription { get; set; }

    [JsonPropertyName("meta_title")]
    public string? MetaTitle { get; set; }

    [JsonPropertyName("meta_description")]
    public string? MetaDescription { get; set; }

    [JsonPropertyName("email_subject")]
    public string? EmailSubject { get; set; }

    [JsonPropertyName("frontmatter")]
    public string? Frontmatter { get; set; }

    [JsonPropertyName("feature_image_alt")]
    public string? FeatureImageAlt { get; set; }

    [JsonPropertyName("feature_image_caption")]
    public string? FeatureImageCaption { get; set; }

    [JsonPropertyName("email_only")]
    public int EmailOnly { get; set; }
}

// ── Import result ────────────────────────────────────────────────────

public sealed class GhostImportResult
{
    public int PostsImported { get; set; }
    public int TagsImported { get; set; }
    public int UsersImported { get; set; }
    public int PostsSkipped { get; set; }
    public int TagsSkipped { get; set; }
    public int UsersSkipped { get; set; }
    public List<string> Errors { get; set; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
