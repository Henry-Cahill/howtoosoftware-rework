using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/posts")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentPostsController(IPostRepository postRepository) : ControllerBase
{
    /// <summary>
    /// GET /api/content/posts/?key={key}&amp;filter=tag:slug+status:published&amp;include=tags,authors&amp;page=1&amp;limit=15&amp;fields=title,slug&amp;order=published_at desc
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPosts(
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

        // Determine status — Content API only exposes published posts by default
        var status = filters.GetValueOrDefault("status", "published");
        string? tagSlug = filters.GetValueOrDefault("tag");
        string? authorSlug = filters.GetValueOrDefault("author");

        PagedResult<Post> result;

        if (tagSlug is not null)
        {
            result = await postRepository.GetPublishedPostsByTagAsync(tagSlug, page, limit, ct);
        }
        else if (authorSlug is not null)
        {
            result = await postRepository.GetPublishedPostsByAuthorAsync(authorSlug, page, limit, ct);
        }
        else
        {
            result = await postRepository.GetAllAsync(status, "post", page, limit, ct);
        }

        var includes = IncludeParser.Parse(include);
        var fieldSet = FieldParser.Parse(fields);

        var posts = result.Items.Select(p => PostMapper.ToResource(p, includes, fieldSet)).ToList();

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

    /// <summary>
    /// GET /api/content/posts/{id_or_slug}/?key={key}&amp;include=tags,authors&amp;fields=title,slug
    /// </summary>
    [HttpGet("{id_or_slug}")]
    public async Task<IActionResult> GetPost(
        string id_or_slug,
        [FromQuery] string? include = null,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        // Try by ID first, then fall back to slug lookup
        var post = await postRepository.GetByIdAsync(id_or_slug, ct)
                ?? await postRepository.GetBySlugAsync(id_or_slug, ct);

        if (post is null)
            return NotFound(new GhostErrorResponse { Errors = [new GhostError { Message = "Post not found.", Type = "NotFoundError" }] });

        var includes = IncludeParser.Parse(include);
        var fieldSet = FieldParser.Parse(fields);
        var resource = PostMapper.ToResource(post, includes, fieldSet);

        return Ok(new GhostEnvelope<PostResource>
        {
            Posts = [resource],
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
