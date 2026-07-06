using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/pages")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentPagesController(IPostRepository postRepository) : ControllerBase
{
    /// <summary>
    /// GET /api/content/pages/?key={key}&amp;filter=tag:slug+status:published&amp;include=tags,authors&amp;page=1&amp;limit=15&amp;fields=title,slug
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPages(
        [FromQuery] string? filter = null,
        [FromQuery] string? include = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 15,
        [FromQuery] string? fields = null,
        [FromQuery] string? order = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var filters = FilterParser.Parse(filter);
        var status = filters.GetValueOrDefault("status", "published");

        var result = await postRepository.GetAllAsync(status, "page", page, limit, ct);

        var includes = IncludeParser.Parse(include);
        var fieldSet = FieldParser.Parse(fields);

        var resources = result.Items.Select(p => PostMapper.ToResource(p, includes, fieldSet)).ToList();

        return Ok(new GhostPagesEnvelope<PostResource>
        {
            Pages = resources,
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
    /// GET /api/content/pages/{id_or_slug}/?key={key}&amp;include=tags,authors&amp;fields=title,slug
    /// </summary>
    [HttpGet("{id_or_slug}")]
    public async Task<IActionResult> GetPage(
        string id_or_slug,
        [FromQuery] string? include = null,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id_or_slug, ct)
                ?? await postRepository.GetBySlugAsync(id_or_slug, ct);

        if (post is null || post.Type != "page")
            return NotFound(new GhostErrorResponse { Errors = [new GhostError { Message = "Page not found.", Type = "NotFoundError" }] });

        var includes = IncludeParser.Parse(include);
        var fieldSet = FieldParser.Parse(fields);
        var resource = PostMapper.ToResource(post, includes, fieldSet);

        return Ok(new GhostPagesEnvelope<PostResource>
        {
            Pages = [resource],
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
