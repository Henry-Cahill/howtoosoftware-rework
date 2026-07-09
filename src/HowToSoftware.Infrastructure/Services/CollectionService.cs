using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public sealed class CollectionService(
    AppDbContext db,
    ISlugGenerator slugGenerator,
    ILogger<CollectionService> logger) : ICollectionService
{
    public async Task<List<Collection>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Collections
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Collection?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Collections
            .Include(c => c.CollectionsPosts.OrderBy(cp => cp.SortOrder))
                .ThenInclude(cp => cp.Post)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Collection?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.Collections
            .Include(c => c.CollectionsPosts.OrderBy(cp => cp.SortOrder))
                .ThenInclude(cp => cp.Post)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);
    }

    public async Task<Collection> CreateAsync(
        CreateCollectionRequest request, CancellationToken ct = default)
    {
        var slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? slugGenerator.GenerateSlug(request.Slug)
            : await slugGenerator.GenerateUniqueSlugAsync(
                request.Title,
                async s => await db.Collections.AnyAsync(c => c.Slug == s, ct),
                ct);

        var now = DateTime.UtcNow;
        var collection = new Collection
        {
            Id = ObjectIdGenerator.New(),
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            Type = request.Type.Trim().ToLowerInvariant(),
            Filter = request.Filter?.Trim(),
            FeatureImage = request.FeatureImage?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Collections.Add(collection);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created collection {CollectionId}: {Title} (type={Type})",
            collection.Id, collection.Title, collection.Type);

        return collection;
    }

    public async Task UpdateAsync(
        string id, UpdateCollectionRequest request, CancellationToken ct = default)
    {
        var collection = await db.Collections.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Collection {id} not found");

        if (!string.IsNullOrWhiteSpace(request.Title))
            collection.Title = request.Title.Trim();

        if (request.Slug is not null)
        {
            var newSlug = slugGenerator.GenerateSlug(request.Slug);
            if (newSlug != collection.Slug)
            {
                var exists = await db.Collections.AnyAsync(
                    c => c.Slug == newSlug && c.Id != id, ct);
                if (exists)
                    throw new InvalidOperationException($"Slug '{newSlug}' is already in use");
                collection.Slug = newSlug;
            }
        }

        if (request.Description is not null)
            collection.Description = request.Description.Trim();
        if (request.Filter is not null)
            collection.Filter = request.Filter.Trim();
        if (request.FeatureImage is not null)
            collection.FeatureImage = request.FeatureImage.Trim();

        collection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated collection {CollectionId}", LogSanitizer.SanitizeForLog(id));
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var collection = await db.Collections.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Collection {id} not found");

        db.Collections.Remove(collection);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted collection {CollectionId}", LogSanitizer.SanitizeForLog(id));
    }

    // ================================================================
    // Posts in collection
    // ================================================================

    public async Task<List<Post>> GetPostsAsync(string collectionId, CancellationToken ct = default)
    {
        var collection = await db.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collectionId, ct)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        if (collection.Type == "automatic")
            return await GetAutomaticPostsAsync(collection, ct);

        // Manual collection — return posts via join table
        return await db.CollectionsPosts
            .Where(cp => cp.CollectionId == collectionId)
            .OrderBy(cp => cp.SortOrder)
            .Include(cp => cp.Post)
                .ThenInclude(p => p.PostsTags.OrderBy(pt => pt.SortOrder))
                    .ThenInclude(pt => pt.Tag)
            .Include(cp => cp.Post)
                .ThenInclude(p => p.PostsAuthors.OrderBy(pa => pa.SortOrder))
                    .ThenInclude(pa => pa.Author)
            .AsSplitQuery()
            .Select(cp => cp.Post)
            .ToListAsync(ct);
    }

    public async Task<List<Post>> PreviewPostsAsync(string? filter, int limit = 10, CancellationToken ct = default)
    {
        if (limit < 1)
            return [];

        return await BuildAutomaticPostQuery(filter)
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddPostAsync(string collectionId, string postId, CancellationToken ct = default)
    {
        var collection = await db.Collections.FindAsync([collectionId], ct)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        if (collection.Type == "automatic")
            throw new InvalidOperationException("Cannot manually add posts to an automatic collection");

        var alreadyLinked = await db.CollectionsPosts
            .AnyAsync(cp => cp.CollectionId == collectionId && cp.PostId == postId, ct);
        if (alreadyLinked) return;

        var maxSort = await db.CollectionsPosts
            .Where(cp => cp.CollectionId == collectionId)
            .MaxAsync(cp => (int?)cp.SortOrder, ct) ?? -1;

        var link = new CollectionsPost
        {
            Id = ObjectIdGenerator.New(),
            CollectionId = collectionId,
            PostId = postId,
            SortOrder = maxSort + 1,
        };

        db.CollectionsPosts.Add(link);
        collection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemovePostAsync(string collectionId, string postId, CancellationToken ct = default)
    {
        var collection = await db.Collections.FindAsync([collectionId], ct)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        if (collection.Type == "automatic")
            throw new InvalidOperationException("Cannot manually remove posts from an automatic collection");

        await db.CollectionsPosts
            .Where(cp => cp.CollectionId == collectionId && cp.PostId == postId)
            .ExecuteDeleteAsync(ct);

        collection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderPostsAsync(
        string collectionId, List<string> orderedPostIds, CancellationToken ct = default)
    {
        var collection = await db.Collections.FindAsync([collectionId], ct)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        if (collection.Type == "automatic")
            throw new InvalidOperationException("Cannot reorder posts in an automatic collection");

        var links = await db.CollectionsPosts
            .Where(cp => cp.CollectionId == collectionId)
            .ToListAsync(ct);

        var linksByPostId = links.ToDictionary(l => l.PostId);

        for (int i = 0; i < orderedPostIds.Count; i++)
        {
            if (linksByPostId.TryGetValue(orderedPostIds[i], out var link))
                link.SortOrder = i;
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetPostCountAsync(string collectionId, CancellationToken ct = default)
    {
        var collection = await db.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collectionId, ct);

        if (collection is null) return 0;

        if (collection.Type == "automatic")
        {
            var query = ApplyFilter(db.Posts.Where(p => p.Status == "published" && p.Type == "post"), collection.Filter);
            return await query.CountAsync(ct);
        }

        return await db.CollectionsPosts
            .CountAsync(cp => cp.CollectionId == collectionId, ct);
    }

    // ================================================================
    // Private helpers
    // ================================================================

    private async Task<List<Post>> GetAutomaticPostsAsync(Collection collection, CancellationToken ct)
    {
        return await BuildAutomaticPostQuery(collection.Filter)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync(ct);
    }

    private IQueryable<Post> BuildAutomaticPostQuery(string? filter)
    {
        var query = db.Posts
            .Where(p => p.Status == "published" && p.Type == "post")
            .Include(p => p.PostsTags.OrderBy(pt => pt.SortOrder))
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostsAuthors.OrderBy(pa => pa.SortOrder))
                .ThenInclude(pa => pa.Author)
            .AsSplitQuery();

        return ApplyFilter(query, filter);
    }

    /// <summary>
    /// Applies Ghost NQL-style filter expressions to a post query.
    /// Supports: tag:{slug}, author:{slug}, status:{value}, featured:true/false, visibility:{value}
    /// Prefix a segment with '-' for negation (e.g. -tag:{slug} = NOT tag).
    /// Filters are AND-combined with '+' separator.
    /// </summary>
    private static IQueryable<Post> ApplyFilter(IQueryable<Post> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;

        foreach (var segment in filter.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var s = segment;
            var negated = s.StartsWith('-');
            if (negated) s = s[1..];

            var colonIdx = s.IndexOf(':');
            if (colonIdx <= 0 || colonIdx >= s.Length - 1) continue;

            var key = s[..colonIdx].Trim().ToLowerInvariant();
            var value = s[(colonIdx + 1)..].Trim();

            query = (key, negated) switch
            {
                ("tag", false) => query.Where(p => p.PostsTags.Any(pt => pt.Tag.Slug == value)),
                ("tag", true) => query.Where(p => !p.PostsTags.Any(pt => pt.Tag.Slug == value)),
                ("author", false) => query.Where(p => p.PostsAuthors.Any(pa => pa.Author.Slug == value)),
                ("author", true) => query.Where(p => !p.PostsAuthors.Any(pa => pa.Author.Slug == value)),
                ("status", false) => query.Where(p => p.Status == value),
                ("status", true) => query.Where(p => p.Status != value),
                ("featured", false) => bool.TryParse(value, out var f)
                    ? query.Where(p => p.Featured == f)
                    : query,
                ("featured", true) => bool.TryParse(value, out var f2)
                    ? query.Where(p => p.Featured != f2)
                    : query,
                ("visibility", false) => query.Where(p => p.Visibility == value),
                ("visibility", true) => query.Where(p => p.Visibility != value),
                _ => query,
            };
        }

        return query;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
