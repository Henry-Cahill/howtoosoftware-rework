using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/tags")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentTagsController(ITagRepository tagRepository) : ControllerBase
{
    /// <summary>
    /// GET /api/content/tags/?key={key}&amp;include=count.posts&amp;page=1&amp;limit=15
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTags(
        [FromQuery] string? include = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 15,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var result = await tagRepository.GetPublicTagsPagedAsync(page, limit, ct);

        var includePostCount = include?.Contains("count.posts", StringComparison.OrdinalIgnoreCase) == true;

        Dictionary<string, int>? postCounts = null;
        if (includePostCount && result.Items.Count > 0)
        {
            var tagIds = result.Items.Select(t => t.Id);
            postCounts = await tagRepository.GetPostCountsAsync(tagIds, ct);
        }

        var tags = result.Items.Select(t =>
        {
            int? count = includePostCount
                ? (postCounts?.GetValueOrDefault(t.Id) ?? 0)
                : null;
            return TagMapper.ToResource(t, count);
        }).ToList();

        return Ok(new GhostTagsEnvelope
        {
            Tags = tags,
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

    /// <summary>
    /// GET /api/content/tags/{id_or_slug}/?key={key}&amp;include=count.posts
    /// </summary>
    [HttpGet("{id_or_slug}")]
    public async Task<IActionResult> GetTag(
        string id_or_slug,
        [FromQuery] string? include = null,
        CancellationToken ct = default)
    {
        var tag = await tagRepository.GetByIdAsync(id_or_slug, ct)
                ?? await tagRepository.GetBySlugAsync(id_or_slug, ct);

        if (tag is null)
            return NotFound(new GhostErrorResponse { Errors = [new GhostError { Message = "Tag not found.", Type = "NotFoundError" }] });

        var includePostCount = include?.Contains("count.posts", StringComparison.OrdinalIgnoreCase) == true;
        int? postCount = null;
        if (includePostCount)
        {
            postCount = await tagRepository.GetPostCountAsync(tag.Id, ct);
        }

        var resource = TagMapper.ToResource(tag, postCount);

        return Ok(new GhostTagsEnvelope
        {
            Tags = [resource],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = 1,
                    Limit = 1,
                    Pages = 1,
                    Total = 1,
                    Next = null,
                    Prev = null,
                }
            }
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
