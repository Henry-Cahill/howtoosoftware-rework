using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ICollectionService
{
    Task<List<Collection>> GetAllAsync(CancellationToken ct = default);
    Task<Collection?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Collection?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Collection> CreateAsync(CreateCollectionRequest request, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateCollectionRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>Gets the posts in a collection, respecting sort order. For automatic collections, applies the filter.</summary>
    Task<List<Post>> GetPostsAsync(string collectionId, CancellationToken ct = default);

    /// <summary>Gets a live preview of posts matching an automatic collection filter.</summary>
    Task<List<Post>> PreviewPostsAsync(string? filter, int limit = 10, CancellationToken ct = default);

    /// <summary>Adds a post to a manual collection.</summary>
    Task AddPostAsync(string collectionId, string postId, CancellationToken ct = default);

    /// <summary>Removes a post from a manual collection.</summary>
    Task RemovePostAsync(string collectionId, string postId, CancellationToken ct = default);

    /// <summary>Reorders posts in a manual collection.</summary>
    Task ReorderPostsAsync(string collectionId, List<string> orderedPostIds, CancellationToken ct = default);

    /// <summary>Gets the count of posts in a collection.</summary>
    Task<int> GetPostCountAsync(string collectionId, CancellationToken ct = default);
}

public record CreateCollectionRequest
{
    public required string Title { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public required string Type { get; init; }
    public string? Filter { get; init; }
    public string? FeatureImage { get; init; }
}

public record UpdateCollectionRequest
{
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? Filter { get; init; }
    public string? FeatureImage { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
