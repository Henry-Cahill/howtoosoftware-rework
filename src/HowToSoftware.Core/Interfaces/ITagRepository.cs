using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Tag>> GetAllAsync(CancellationToken ct = default);
    Task<List<Tag>> GetPublicTagsAsync(CancellationToken ct = default);
    Task<PagedResult<Tag>> GetPublicTagsPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetPostCountsAsync(IEnumerable<string> tagIds, CancellationToken ct = default);
    Task<List<Tag>> GetTagsByPostIdAsync(string postId, CancellationToken ct = default);
    Task<int> GetPostCountAsync(string tagId, CancellationToken ct = default);
    Task<List<(DateTime Month, int Count)>> GetMonthlyPostCountsAsync(string tagId, int months = 6, CancellationToken ct = default);
    Task AddAsync(Tag tag, CancellationToken ct = default);
    Task UpdateAsync(Tag tag, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task MergeAsync(string sourceTagId, string targetTagId, CancellationToken ct = default);
    Task UpdateSortOrderAsync(List<(string Id, int SortOrder)> tagOrders, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
