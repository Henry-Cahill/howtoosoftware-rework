using System.Text.Json.Serialization;

namespace HowToSoftware.Web.Models.Api;

// --- Ghost-compatible JSON envelope ---

public sealed class GhostErrorResponse
{
    [JsonPropertyName("errors")]
    public required List<GhostError> Errors { get; init; }
}

public sealed class GhostError
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }
}

public sealed class GhostEnvelope<T>
{
    [JsonPropertyName("posts")]
    public required List<T> Posts { get; init; }

    [JsonPropertyName("meta")]
    public required GhostMeta Meta { get; init; }
}

public sealed class GhostPagesEnvelope<T>
{
    [JsonPropertyName("pages")]
    public required List<T> Pages { get; init; }

    [JsonPropertyName("meta")]
    public required GhostMeta Meta { get; init; }
}

public sealed class GhostTagsEnvelope
{
    [JsonPropertyName("tags")]
    public required List<TagResource> Tags { get; init; }

    [JsonPropertyName("meta")]
    public required GhostMeta Meta { get; init; }
}

public sealed class GhostAuthorsEnvelope
{
    [JsonPropertyName("authors")]
    public required List<AuthorResource> Authors { get; init; }

    [JsonPropertyName("meta")]
    public required GhostMeta Meta { get; init; }
}

public sealed class GhostMeta
{
    [JsonPropertyName("pagination")]
    public required GhostPagination Pagination { get; init; }
}

public sealed class GhostPagination
{
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("pages")]
    public int Pages { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("next")]
    public int? Next { get; init; }

    [JsonPropertyName("prev")]
    public int? Prev { get; init; }
}

// --- Post resource (Ghost Content API shape) ---

public sealed class PostResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("uuid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uuid { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("slug")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Slug { get; init; }

    [JsonPropertyName("html")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Html { get; init; }

    [JsonPropertyName("comment_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CommentId { get; init; }

    [JsonPropertyName("feature_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FeatureImage { get; init; }

    [JsonPropertyName("featured")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Featured { get; init; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; init; }

    [JsonPropertyName("visibility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Visibility { get; init; }

    [JsonPropertyName("created_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? UpdatedAt { get; init; }

    [JsonPropertyName("published_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? PublishedAt { get; init; }

    [JsonPropertyName("custom_excerpt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomExcerpt { get; init; }

    [JsonPropertyName("codeinjection_head")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeinjectionHead { get; init; }

    [JsonPropertyName("codeinjection_foot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeinjectionFoot { get; init; }

    [JsonPropertyName("custom_template")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomTemplate { get; init; }

    [JsonPropertyName("canonical_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CanonicalUrl { get; init; }

    [JsonPropertyName("excerpt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Excerpt { get; init; }

    [JsonPropertyName("reading_time")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ReadingTime { get; init; }

    [JsonPropertyName("og_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgImage { get; init; }

    [JsonPropertyName("og_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgTitle { get; init; }

    [JsonPropertyName("og_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgDescription { get; init; }

    [JsonPropertyName("twitter_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterImage { get; init; }

    [JsonPropertyName("twitter_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterTitle { get; init; }

    [JsonPropertyName("twitter_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterDescription { get; init; }

    [JsonPropertyName("meta_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaTitle { get; init; }

    [JsonPropertyName("meta_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaDescription { get; init; }

    [JsonPropertyName("email_subject")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EmailSubject { get; init; }

    [JsonPropertyName("frontmatter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Frontmatter { get; init; }

    [JsonPropertyName("feature_image_alt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FeatureImageAlt { get; init; }

    [JsonPropertyName("feature_image_caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FeatureImageCaption { get; init; }

    // Included relations (populated when ?include=tags,authors)
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TagResource>? Tags { get; init; }

    [JsonPropertyName("authors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AuthorResource>? Authors { get; init; }

    // Ghost Content API also exposes primary_tag and primary_author shortcuts
    [JsonPropertyName("primary_tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TagResource? PrimaryTag { get; init; }

    [JsonPropertyName("primary_author")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthorResource? PrimaryAuthor { get; init; }
}

// --- Tag resource ---

public sealed class TagResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("slug")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Slug { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("feature_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FeatureImage { get; init; }

    [JsonPropertyName("visibility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Visibility { get; init; }

    [JsonPropertyName("og_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgImage { get; init; }

    [JsonPropertyName("og_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgTitle { get; init; }

    [JsonPropertyName("og_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgDescription { get; init; }

    [JsonPropertyName("twitter_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterImage { get; init; }

    [JsonPropertyName("twitter_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterTitle { get; init; }

    [JsonPropertyName("twitter_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterDescription { get; init; }

    [JsonPropertyName("meta_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaTitle { get; init; }

    [JsonPropertyName("meta_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaDescription { get; init; }

    [JsonPropertyName("codeinjection_head")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeinjectionHead { get; init; }

    [JsonPropertyName("codeinjection_foot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeinjectionFoot { get; init; }

    [JsonPropertyName("canonical_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CanonicalUrl { get; init; }

    [JsonPropertyName("accent_color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccentColor { get; init; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }

    [JsonPropertyName("count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TagCountResource? Count { get; init; }
}

public sealed class TagCountResource
{
    [JsonPropertyName("posts")]
    public int Posts { get; init; }
}

// --- Author resource ---

public sealed class AuthorResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("slug")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Slug { get; init; }

    [JsonPropertyName("profile_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProfileImage { get; init; }

    [JsonPropertyName("cover_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CoverImage { get; init; }

    [JsonPropertyName("bio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Bio { get; init; }

    [JsonPropertyName("website")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Website { get; init; }

    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Location { get; init; }

    [JsonPropertyName("facebook")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Facebook { get; init; }

    [JsonPropertyName("twitter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Twitter { get; init; }

    [JsonPropertyName("meta_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaTitle { get; init; }

    [JsonPropertyName("meta_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaDescription { get; init; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }

    [JsonPropertyName("count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthorCountResource? Count { get; init; }
}

public sealed class AuthorCountResource
{
    [JsonPropertyName("posts")]
    public int Posts { get; init; }
}

// --- Settings envelope (Ghost Content API shape) ---

public sealed class GhostSettingsEnvelope
{
    [JsonPropertyName("settings")]
    public required SettingsResource Settings { get; init; }

    [JsonPropertyName("meta")]
    public required object Meta { get; init; }
}

public sealed class SettingsResource
{
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("logo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Logo { get; init; }

    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }

    [JsonPropertyName("accent_color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccentColor { get; init; }

    [JsonPropertyName("cover_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CoverImage { get; init; }

    [JsonPropertyName("facebook")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Facebook { get; init; }

    [JsonPropertyName("twitter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Twitter { get; init; }

    [JsonPropertyName("lang")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Lang { get; init; }

    [JsonPropertyName("timezone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Timezone { get; init; }

    [JsonPropertyName("codeinjection_head")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeinjectionHead { get; init; }

    [JsonPropertyName("codeinjection_foot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeinjectionFoot { get; init; }

    [JsonPropertyName("navigation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Navigation { get; init; }

    [JsonPropertyName("secondary_navigation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? SecondaryNavigation { get; init; }

    [JsonPropertyName("meta_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaTitle { get; init; }

    [JsonPropertyName("meta_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetaDescription { get; init; }

    [JsonPropertyName("og_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgImage { get; init; }

    [JsonPropertyName("og_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgTitle { get; init; }

    [JsonPropertyName("og_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgDescription { get; init; }

    [JsonPropertyName("twitter_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterImage { get; init; }

    [JsonPropertyName("twitter_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterTitle { get; init; }

    [JsonPropertyName("twitter_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TwitterDescription { get; init; }

    [JsonPropertyName("members_support_address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MembersSupportAddress { get; init; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
