using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class PostRepository(AppDbContext db) : IPostRepository
{
    public async Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);
    }

    public async Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Uuid == uuid, ct);
    }

    public async Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = PostsWithIncludes()
            .Where(p => p.Status == "published" && p.Type == "post")
            .OrderByDescending(p => p.PublishedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var query = PostsWithIncludes()
            .Where(p => p.Status == "published" && p.Type == "post"
                && p.PostsTags.Any(pt => pt.Tag.Slug == tagSlug))
            .OrderByDescending(p => p.PublishedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var query = PostsWithIncludes()
            .Where(p => p.Status == "published" && p.Type == "post"
                && p.PostsAuthors.Any(pa => pa.Author.Slug == authorSlug))
            .OrderByDescending(p => p.PublishedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = PostsWithIncludes()
            .Where(p => p.Status == "published" && p.Type == "post"
                && p.NewsletterId == newsletterId)
            .OrderByDescending(p => p.PublishedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default)
    {
        var query = PostsWithIncludes().AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);

        query = query.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .Where(p => p.Featured && p.Status == "published" && p.Type == "post")
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default)
    {
        var tagIds = await db.PostsTags
            .Where(pt => pt.PostId == postId)
            .Select(pt => pt.TagId)
            .ToListAsync(ct);

        if (tagIds.Count == 0)
        {
            return await PostsWithIncludes()
                .Where(p => p.Id != postId && p.Status == "published" && p.Type == "post")
                .OrderByDescending(p => p.PublishedAt)
                .Take(count)
                .ToListAsync(ct);
        }

        return await PostsWithIncludes()
            .Where(p => p.Id != postId && p.Status == "published" && p.Type == "post"
                && p.PostsTags.Any(pt => tagIds.Contains(pt.TagId)))
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .Include(p => p.Revisions.OrderByDescending(r => r.CreatedAt))
            .Include(p => p.MobiledocRevisions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task AddAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Update(post);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Posts.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
    {
        await db.Posts.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default)
    {
        await db.Posts.Where(p => ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Featured, featured), ct);
    }

    public async Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default)
    {
        var query = db.Posts.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);

        return await query.CountAsync(ct);
    }

    public async Task<List<Post>> GetAllPagesAsync(CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .Where(p => p.Type == "page")
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync(ct);
    }

    public async Task<List<Post>> GetPublishedPagesAsync(CancellationToken ct = default)
    {
        return await PostsWithIncludes()
            .Where(p => p.Type == "page" && p.Status == "published")
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync(ct);
    }

    public async Task UpdateSortOrderAsync(IReadOnlyList<(string Id, string? ParentId, int SortOrder)> updates, CancellationToken ct = default)
    {
        foreach (var (id, parentId, sortOrder) in updates)
        {
            await db.Posts.Where(p => p.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.ParentId, parentId)
                    .SetProperty(p => p.SortOrder, sortOrder)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        }
    }

    private IQueryable<Post> PostsWithIncludes()
    {
        return db.Posts
            .Include(p => p.PostsTags.OrderBy(pt => pt.SortOrder))
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostsAuthors.OrderBy(pa => pa.SortOrder))
                .ThenInclude(pa => pa.Author)
            .Include(p => p.PostsProducts)
            .Include(p => p.Meta)
            .AsSplitQuery();
    }

    private static async Task<PagedResult<Post>> ToPagedResultAsync(
        IQueryable<Post> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Post>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
