using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/authors")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentAuthorsController(IUserRepository userRepository) : ControllerBase
{
    /// <summary>
    /// GET /api/content/authors/?key={key}&amp;include=count.posts&amp;page=1&amp;limit=15
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] string? include = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 15,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var result = await userRepository.GetActiveAuthorsPagedAsync(page, limit, ct);

        var includePostCount = include?.Contains("count.posts", StringComparison.OrdinalIgnoreCase) == true;

        Dictionary<string, int>? postCounts = null;
        if (includePostCount && result.Items.Count > 0)
        {
            var userIds = result.Items.Select(u => u.Id);
            postCounts = await userRepository.GetPostCountsAsync(userIds, ct);
        }

        var authors = result.Items.Select(u =>
        {
            int? count = includePostCount
                ? (postCounts?.GetValueOrDefault(u.Id) ?? 0)
                : null;
            return AuthorMapper.ToResource(u, count);
        }).ToList();

        return Ok(new GhostAuthorsEnvelope
        {
            Authors = authors,
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
    /// GET /api/content/authors/{id_or_slug}/?key={key}&amp;include=count.posts
    /// </summary>
    [HttpGet("{id_or_slug}")]
    public async Task<IActionResult> GetAuthor(
        string id_or_slug,
        [FromQuery] string? include = null,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id_or_slug, ct)
                ?? await userRepository.GetBySlugAsync(id_or_slug, ct);

        if (user is null)
            return NotFound(new GhostErrorResponse { Errors = [new GhostError { Message = "Author not found.", Type = "NotFoundError" }] });

        var includePostCount = include?.Contains("count.posts", StringComparison.OrdinalIgnoreCase) == true;
        int? postCount = null;
        if (includePostCount)
        {
            postCount = await userRepository.GetPostCountAsync(user.Id, ct);
        }

        var resource = AuthorMapper.ToResource(user, postCount);

        return Ok(new GhostAuthorsEnvelope
        {
            Authors = [resource],
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
