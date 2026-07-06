using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IContentService
{
    // Create
    Task<Post> CreateAsync(ContentCreateRequest request, string authorId, CancellationToken ct = default);

    // Update
    Task<Post> UpdateAsync(string id, ContentUpdateRequest request, string editorId, CancellationToken ct = default);

    // Status transitions
    Task<Post> PublishAsync(string id, string publisherId, CancellationToken ct = default);
    Task<Post> UnpublishAsync(string id, CancellationToken ct = default);
    Task<Post> ScheduleAsync(string id, DateTime scheduledAt, string publisherId, CancellationToken ct = default);

    // Send as email
    Task<Email> SendAsEmailAsync(string postId, string newsletterId, string recipientFilter, CancellationToken ct = default);

    // Duplicate
    Task<Post> DuplicateAsync(string id, string authorId, CancellationToken ct = default);

    // Delete
    Task DeleteAsync(string id, CancellationToken ct = default);

    // Revisions
    Task<List<PostRevision>> GetRevisionsAsync(string postId, CancellationToken ct = default);
    Task<Post> RestoreRevisionAsync(string postId, string revisionId, string editorId, CancellationToken ct = default);
}

public record ContentCreateRequest
{
    public required string Title { get; init; }
    public string Type { get; init; } = "post";
    public string? Slug { get; init; }
    public string? Lexical { get; init; }
    public string? Mobiledoc { get; init; }
    public bool Featured { get; init; }
    public string? FeatureImage { get; init; }
    public string? CustomExcerpt { get; init; }
    public string Visibility { get; init; } = "public";
    public string? Locale { get; init; }
    public string? CodeinjectionHead { get; init; }
    public string? CodeinjectionFoot { get; init; }
    public string? CustomTemplate { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? ParentId { get; init; }
    public List<string>? TagIds { get; init; }
    public PostMetaRequest? Meta { get; init; }
}

public record ContentUpdateRequest
{
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? Lexical { get; init; }
    public string? Mobiledoc { get; init; }
    public bool? Featured { get; init; }
    public string? FeatureImage { get; init; }
    public string? CustomExcerpt { get; init; }
    public string? Visibility { get; init; }
    public string? Locale { get; init; }
    public string? CodeinjectionHead { get; init; }
    public string? CodeinjectionFoot { get; init; }
    public string? CustomTemplate { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? ParentId { get; init; }
    public bool ClearParent { get; init; }
    public List<string>? TagIds { get; init; }
    public PostMetaRequest? Meta { get; init; }
    public string? RevisionReason { get; init; }
}

public record PostMetaRequest
{
    public string? OgImage { get; init; }
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? TwitterImage { get; init; }
    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? EmailSubject { get; init; }
    /// <summary>Optional alternate subject line for A/B testing the newsletter send.</summary>
    public string? EmailSubjectB { get; init; }
    public string? Frontmatter { get; init; }
    public string? FeatureImageAlt { get; init; }
    public string? FeatureImageCaption { get; init; }
    public bool EmailOnly { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
