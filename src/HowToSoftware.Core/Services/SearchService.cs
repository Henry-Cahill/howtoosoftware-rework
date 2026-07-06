using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

public class SearchService(ISearchRepository searchRepository) : ISearchService
{
    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var query = request.Query.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return new SearchResult
            {
                Posts = [],
                Page = 1,
                PageSize = request.PageSize,
                TotalCount = 0,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            };
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var result = await searchRepository.SearchPostsAsync(query, request.Type, page, pageSize, ct);

        return new SearchResult
        {
            Posts = result.Items,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
