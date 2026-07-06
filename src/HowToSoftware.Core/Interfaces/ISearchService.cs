using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct = default);
}

public interface ISearchRepository
{
    Task<PagedResult<Post>> SearchPostsAsync(string query, string? type, int page, int pageSize, CancellationToken ct = default);
}

public record SearchRequest
{
    public required string Query { get; init; }
    public string? Type { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 15;
}

public record SearchResult
{
    public required IReadOnlyList<Post> Posts { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
