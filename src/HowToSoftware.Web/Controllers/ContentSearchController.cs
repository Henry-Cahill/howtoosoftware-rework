using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/search")]
[AllowAnonymous]
[EnableRateLimiting("content-api")]
public class ContentSearchController(ISearchService searchService) : ControllerBase
{
    /// <summary>
    /// GET /api/content/search/?key={key}&amp;q=search+terms&amp;type=post&amp;page=1&amp;limit=15&amp;include=tags,authors&amp;fields=title,slug
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? type = null,
        [FromQuery] string? include = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 15,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new GhostEnvelope<PostResource>
            {
                Posts = [],
                Meta = new GhostMeta
                {
                    Pagination = new GhostPagination
                    {
                        Page = 1, Limit = limit, Pages = 0, Total = 0
                    }
                }
            });
        }

        if (page < 1) page = 1;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var result = await searchService.SearchAsync(new SearchRequest
        {
            Query = q,
            Type = type,
            Page = page,
            PageSize = limit
        }, ct);

        var includes = IncludeParser.Parse(include);
        var fieldSet = FieldParser.Parse(fields);
        var posts = result.Posts.Select(p => PostMapper.ToResource(p, includes, fieldSet)).ToList();

        return Ok(new GhostEnvelope<PostResource>
        {
            Posts = posts,
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = result.Page,
                    Limit = limit,
                    Pages = result.TotalPages,
                    Total = result.TotalCount,
                    Next = result.HasNextPage ? result.Page + 1 : null,
                    Prev = result.HasPreviousPage ? result.Page - 1 : null,
                }
            }
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
