using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default);
    Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default);
    /// <summary>
    /// Published posts linked to a newsletter (Posts.NewsletterId == newsletterId).
    /// Ordered by PublishedAt descending. Used by the public newsletter archive page.
    /// </summary>
    Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default);
    Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default);
    Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default);
    Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default);
    Task AddAsync(Post post, CancellationToken ct = default);
    Task UpdateAsync(Post post, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default);
    Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default);
    Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default);
    Task<List<Post>> GetAllPagesAsync(CancellationToken ct = default);
    Task<List<Post>> GetPublishedPagesAsync(CancellationToken ct = default);
    Task UpdateSortOrderAsync(IReadOnlyList<(string Id, string? ParentId, int SortOrder)> updates, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
