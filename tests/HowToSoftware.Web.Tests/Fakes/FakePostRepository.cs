using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Tests.Fakes;

internal class FakePostRepository : IPostRepository
{
    public List<Post> Posts { get; } = [];

    public Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));

    public Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => Task.FromResult(Posts.FirstOrDefault(p => p.Slug == slug));

    public Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default)
        => Task.FromResult(Posts.FirstOrDefault(p => p.Uuid == uuid));

    public Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var published = Posts.Where(p => p.Status == "published" && p.Type == "post")
            .OrderByDescending(p => p.PublishedAt).ToList();
        return Task.FromResult(Paginate(published, page, pageSize));
    }

    public Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var posts = Posts.Where(p => p.Status == "published" && p.Type == "post"
            && p.PostsTags.Any(pt => pt.Tag.Slug == tagSlug))
            .OrderByDescending(p => p.PublishedAt).ToList();
        return Task.FromResult(Paginate(posts, page, pageSize));
    }

    public Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default)
    {
        var posts = Posts.Where(p => p.Status == "published" && p.Type == "post"
            && p.NewsletterId == newsletterId)
            .OrderByDescending(p => p.PublishedAt).ToList();
        return Task.FromResult(Paginate(posts, page, pageSize));
    }

    public Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var posts = Posts.Where(p => p.Status == "published" && p.Type == "post"
            && p.PostsAuthors.Any(pa => pa.Author.Slug == authorSlug))
            .OrderByDescending(p => p.PublishedAt).ToList();
        return Task.FromResult(Paginate(posts, page, pageSize));
    }

    public Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default)
    {
        var filtered = Posts.AsEnumerable();
        if (status is not null) filtered = filtered.Where(p => p.Status == status);
        if (type is not null) filtered = filtered.Where(p => p.Type == type);
        return Task.FromResult(Paginate(filtered.ToList(), page, pageSize));
    }

    public Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default)
        => Task.FromResult(Posts.Where(p => p.Featured).Take(count).ToList());

    public Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default)
        => Task.FromResult(Posts.Where(p => p.Id != postId && p.Status == "published").Take(count).ToList());

    public Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default)
        => GetByIdAsync(id, ct);

    public Task AddAsync(Post post, CancellationToken ct = default)
    { Posts.Add(post); return Task.CompletedTask; }

    public Task UpdateAsync(Post post, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken ct = default)
    { Posts.RemoveAll(p => p.Id == id); return Task.CompletedTask; }

    public Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
    { Posts.RemoveAll(p => ids.Contains(p.Id)); return Task.CompletedTask; }

    public Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default)
    {
        foreach (var p in Posts.Where(p => ids.Contains(p.Id))) p.Featured = featured;
        return Task.CompletedTask;
    }

    public Task<List<Post>> GetAllPagesAsync(CancellationToken ct = default)
        => Task.FromResult(Posts.Where(p => p.Type == "page").ToList());

    public Task<List<Post>> GetPublishedPagesAsync(CancellationToken ct = default)
        => Task.FromResult(Posts.Where(p => p.Type == "page" && p.Status == "published").OrderBy(p => p.SortOrder).ThenBy(p => p.Title).ToList());

    public Task UpdateSortOrderAsync(IReadOnlyList<(string Id, string? ParentId, int SortOrder)> updates, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default)
    {
        var q = Posts.AsEnumerable();
        if (status is not null) q = q.Where(p => p.Status == status);
        if (type is not null) q = q.Where(p => p.Type == type);
        return Task.FromResult(q.Count());
    }

    private static PagedResult<Post> Paginate(List<Post> all, int page, int pageSize)
    {
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<Post>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count,
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
