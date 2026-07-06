using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Tests.Fakes;

internal class FakeTagRepository : ITagRepository
{
    public List<Tag> Tags { get; } = [];

    public Task<Tag?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Tags.FirstOrDefault(t => t.Id == id));

    public Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => Task.FromResult(Tags.FirstOrDefault(t => t.Slug == slug));

    public Task<List<Tag>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Tags.ToList());

    public Task<List<Tag>> GetPublicTagsAsync(CancellationToken ct = default)
        => Task.FromResult(Tags.Where(t => t.Visibility == "public").ToList());

    public Task<PagedResult<Tag>> GetPublicTagsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var publicTags = Tags.Where(t => t.Visibility == "public").ToList();
        var items = publicTags.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<Tag>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = publicTags.Count,
        });
    }

    public Task<Dictionary<string, int>> GetPostCountsAsync(IEnumerable<string> tagIds, CancellationToken ct = default)
        => Task.FromResult(tagIds.ToDictionary(id => id, id =>
            Tags.FirstOrDefault(t => t.Id == id)?.PostsTags.Count ?? 0));

    public Task<List<Tag>> GetTagsByPostIdAsync(string postId, CancellationToken ct = default)
        => Task.FromResult(Tags.Where(t => t.PostsTags.Any(pt => pt.PostId == postId)).ToList());

    public Task<int> GetPostCountAsync(string tagId, CancellationToken ct = default)
        => Task.FromResult(Tags.FirstOrDefault(t => t.Id == tagId)?.PostsTags.Count ?? 0);

    public Task AddAsync(Tag tag, CancellationToken ct = default)
    { Tags.Add(tag); return Task.CompletedTask; }

    public Task UpdateAsync(Tag tag, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken ct = default)
    { Tags.RemoveAll(t => t.Id == id); return Task.CompletedTask; }

    public Task MergeAsync(string sourceTagId, string targetTagId, CancellationToken ct = default)
    {
        var source = Tags.FirstOrDefault(t => t.Id == sourceTagId);
        var target = Tags.FirstOrDefault(t => t.Id == targetTagId);
        if (source is not null && target is not null)
        {
            foreach (var pt in source.PostsTags)
            {
                if (!target.PostsTags.Any(tp => tp.PostId == pt.PostId))
                {
                    pt.TagId = targetTagId;
                    target.PostsTags.Add(pt);
                }
            }
            Tags.Remove(source);
        }
        return Task.CompletedTask;
    }

    public Task<List<(DateTime Month, int Count)>> GetMonthlyPostCountsAsync(string tagId, int months = 6, CancellationToken ct = default)
        => Task.FromResult(new List<(DateTime Month, int Count)>());

    public Task UpdateSortOrderAsync(List<(string Id, int SortOrder)> tagOrders, CancellationToken ct = default)
    {
        foreach (var (id, sortOrder) in tagOrders)
        {
            var tag = Tags.FirstOrDefault(t => t.Id == id);
            if (tag is not null) tag.SortOrder = sortOrder;
        }
        return Task.CompletedTask;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
