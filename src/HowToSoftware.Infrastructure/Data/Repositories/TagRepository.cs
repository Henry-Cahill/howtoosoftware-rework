using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class TagRepository(AppDbContext db) : ITagRepository
{
    public async Task<Tag?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
    }

    public async Task<List<Tag>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Tags.OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<List<Tag>> GetPublicTagsAsync(CancellationToken ct = default)
    {
        return await db.Tags
            .Where(t => t.Visibility == "public")
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Tag>> GetPublicTagsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Tags
            .Where(t => t.Visibility == "public")
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Tag>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<Dictionary<string, int>> GetPostCountsAsync(IEnumerable<string> tagIds, CancellationToken ct = default)
    {
        return await db.PostsTags
            .Where(pt => tagIds.Contains(pt.TagId)
                && pt.Post.Status == "published"
                && pt.Post.Type == "post")
            .GroupBy(pt => pt.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TagId, x => x.Count, ct);
    }

    public async Task<List<Tag>> GetTagsByPostIdAsync(string postId, CancellationToken ct = default)
    {
        return await db.PostsTags
            .Where(pt => pt.PostId == postId)
            .OrderBy(pt => pt.SortOrder)
            .Select(pt => pt.Tag)
            .ToListAsync(ct);
    }

    public async Task<int> GetPostCountAsync(string tagId, CancellationToken ct = default)
    {
        return await db.PostsTags
            .Where(pt => pt.TagId == tagId
                && pt.Post.Status == "published"
                && pt.Post.Type == "post")
            .CountAsync(ct);
    }

    public async Task<List<(DateTime Month, int Count)>> GetMonthlyPostCountsAsync(string tagId, int months = 6, CancellationToken ct = default)
    {
        var cutoff = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));

        var data = await db.PostsTags
            .Where(pt => pt.TagId == tagId
                && pt.Post.Status == "published"
                && pt.Post.Type == "post"
                && pt.Post.PublishedAt != null
                && pt.Post.PublishedAt >= cutoff)
            .GroupBy(pt => new { pt.Post.PublishedAt!.Value.Year, pt.Post.PublishedAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        // Fill in all months (including zeros)
        var result = new List<(DateTime Month, int Count)>();
        for (var i = 0; i < months; i++)
        {
            var month = cutoff.AddMonths(i);
            var count = data.FirstOrDefault(d => d.Year == month.Year && d.Month == month.Month)?.Count ?? 0;
            result.Add((month, count));
        }
        return result;
    }

    public async Task AddAsync(Tag tag, CancellationToken ct = default)
    {
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tag tag, CancellationToken ct = default)
    {
        db.Tags.Update(tag);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Tags.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task MergeAsync(string sourceTagId, string targetTagId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Find posts already associated with the target tag to avoid duplicates
        var targetPostIds = await db.PostsTags
            .Where(pt => pt.TagId == targetTagId)
            .Select(pt => pt.PostId)
            .ToListAsync(ct);

        // Reassign non-duplicate post associations from source to target
        await db.PostsTags
            .Where(pt => pt.TagId == sourceTagId && !targetPostIds.Contains(pt.PostId))
            .ExecuteUpdateAsync(s => s.SetProperty(pt => pt.TagId, targetTagId), ct);

        // Delete remaining source associations (duplicates)
        await db.PostsTags
            .Where(pt => pt.TagId == sourceTagId)
            .ExecuteDeleteAsync(ct);

        // Delete the source tag
        await db.Tags.Where(t => t.Id == sourceTagId).ExecuteDeleteAsync(ct);

        await tx.CommitAsync(ct);
    }

    public async Task UpdateSortOrderAsync(List<(string Id, int SortOrder)> tagOrders, CancellationToken ct = default)
    {
        foreach (var (id, sortOrder) in tagOrders)
        {
            await db.Tags
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.SortOrder, sortOrder), ct);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
