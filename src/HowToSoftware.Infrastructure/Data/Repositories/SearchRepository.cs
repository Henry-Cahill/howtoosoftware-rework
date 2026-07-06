using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class SearchRepository(AppDbContext db) : ISearchRepository
{
    public async Task<PagedResult<Post>> SearchPostsAsync(
        string query, string? type, int page, int pageSize, CancellationToken ct = default)
    {
        // Use SQL Server full-text search via FREETEXT for natural-language matching.
        // Falls back to LIKE-based search if the full-text index is not available.
        var ftsAvailable = await IsFullTextIndexAvailableAsync(ct);

        IQueryable<Post> searchQuery;

        if (ftsAvailable)
        {
            // FREETEXT handles stemming, word-breaking and noise-word removal automatically.
            // We search across title, plaintext, and custom_excerpt columns.
            searchQuery = db.Posts
                .FromSqlInterpolated($"""
                    SELECT p.* FROM posts p
                    WHERE FREETEXT((p.title, p.plaintext, p.custom_excerpt), {query})
                      AND p.status = 'published'
                    """);
        }
        else
        {
            // Fallback: simple LIKE-based search when FTS is not set up
            searchQuery = db.Posts
                .Where(p => p.Status == "published"
                    && (EF.Functions.Like(p.Title, $"%{query}%")
                        || (p.Plaintext != null && EF.Functions.Like(p.Plaintext, $"%{query}%"))
                        || (p.CustomExcerpt != null && EF.Functions.Like(p.CustomExcerpt, $"%{query}%"))));
        }

        if (!string.IsNullOrEmpty(type))
            searchQuery = searchQuery.Where(p => p.Type == type);

        // Apply includes for navigation properties
        searchQuery = searchQuery
            .Include(p => p.PostsTags.OrderBy(pt => pt.SortOrder))
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostsAuthors.OrderBy(pa => pa.SortOrder))
                .ThenInclude(pa => pa.Author)
            .Include(p => p.Meta)
            .AsSplitQuery()
            .OrderByDescending(p => p.PublishedAt);

        var totalCount = await searchQuery.CountAsync(ct);
        var items = await searchQuery
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

    private async Task<bool> IsFullTextIndexAvailableAsync(CancellationToken ct)
    {
        var result = await db.Database
            .SqlQueryRaw<int>("""
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID('posts')
                ) THEN 1 ELSE 0 END AS [Value]
                """)
            .FirstOrDefaultAsync(ct);

        return result == 1;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
