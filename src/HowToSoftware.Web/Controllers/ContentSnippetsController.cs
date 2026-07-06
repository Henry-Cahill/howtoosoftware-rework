using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/snippets")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentSnippetsController(
    ISnippetService snippetService) : ControllerBase
{
    /// <summary>
    /// GET /api/content/snippets — returns all snippets.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSnippets(CancellationToken ct)
    {
        var snippets = await snippetService.GetAllAsync(ct);

        return Ok(new SnippetsEnvelope
        {
            Snippets = snippets.Select(ToResource).ToList(),
            Meta = new SnippetsMeta
            {
                Pagination = new SnippetsPagination
                {
                    Page = 1,
                    Limit = snippets.Count,
                    Pages = 1,
                    Total = snippets.Count,
                }
            }
        });
    }

    /// <summary>
    /// GET /api/content/snippets/{id_or_name} — returns a single snippet by ID or name.
    /// </summary>
    [HttpGet("{idOrName}")]
    public async Task<IActionResult> GetSnippet(string idOrName, CancellationToken ct)
    {
        var snippet = await snippetService.GetByIdAsync(idOrName, ct)
                   ?? await snippetService.GetByNameAsync(idOrName, ct);

        if (snippet is null)
            return NotFound(new { errors = new[] { new { message = "Snippet not found", type = "NotFoundError" } } });

        return Ok(new SnippetsEnvelope
        {
            Snippets = [ToResource(snippet)],
            Meta = new SnippetsMeta
            {
                Pagination = new SnippetsPagination { Page = 1, Limit = 1, Pages = 1, Total = 1 }
            }
        });
    }

    private static SnippetResource ToResource(Core.Entities.Snippet s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Mobiledoc = s.Mobiledoc,
        Lexical = s.Lexical,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
    };
}

// --- Response DTOs ---

public sealed class SnippetsEnvelope
{
    [JsonPropertyName("snippets")]
    public required List<SnippetResource> Snippets { get; init; }

    [JsonPropertyName("meta")]
    public required SnippetsMeta Meta { get; init; }
}

public sealed class SnippetsMeta
{
    [JsonPropertyName("pagination")]
    public required SnippetsPagination Pagination { get; init; }
}

public sealed class SnippetsPagination
{
    [JsonPropertyName("page")]
    public int Page { get; init; }
    [JsonPropertyName("limit")]
    public int Limit { get; init; }
    [JsonPropertyName("pages")]
    public int Pages { get; init; }
    [JsonPropertyName("total")]
    public int Total { get; init; }
}

public sealed class SnippetResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("mobiledoc")]
    public string Mobiledoc { get; init; } = null!;

    [JsonPropertyName("lexical")]
    public string? Lexical { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
