using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/content/collections")]
[Authorize(Policy = "ContentApi")]
[EnableRateLimiting("content-api")]
public class ContentCollectionsController(
    ICollectionService collectionService) : ControllerBase
{
    /// <summary>
    /// GET /api/content/collections — returns all collections.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCollections(CancellationToken ct)
    {
        var collections = await collectionService.GetAllAsync(ct);

        return Ok(new CollectionsEnvelope
        {
            Collections = collections.Select(ToResource).ToList(),
            Meta = new CollectionsMeta
            {
                Pagination = new CollectionsPagination
                {
                    Page = 1,
                    Limit = collections.Count,
                    Pages = 1,
                    Total = collections.Count,
                }
            }
        });
    }

    /// <summary>
    /// GET /api/content/collections/{id_or_slug} — returns a single collection with its posts.
    /// </summary>
    [HttpGet("{idOrSlug}")]
    public async Task<IActionResult> GetCollection(string idOrSlug, CancellationToken ct)
    {
        var collection = await collectionService.GetByIdAsync(idOrSlug, ct)
                      ?? await collectionService.GetBySlugAsync(idOrSlug, ct);

        if (collection is null)
            return NotFound(new { errors = new[] { new { message = "Collection not found", type = "NotFoundError" } } });

        var posts = await collectionService.GetPostsAsync(collection.Id, ct);

        var resource = ToResource(collection);
        resource.Posts = posts.Select(p => new CollectionPostResource
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            FeatureImage = p.FeatureImage,
            PublishedAt = p.PublishedAt,
        }).ToList();

        return Ok(new CollectionsEnvelope
        {
            Collections = [resource],
            Meta = new CollectionsMeta
            {
                Pagination = new CollectionsPagination { Page = 1, Limit = 1, Pages = 1, Total = 1 }
            }
        });
    }

    private static CollectionResource ToResource(Collection c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Slug = c.Slug,
        Description = c.Description,
        Type = c.Type,
        Filter = c.Filter,
        FeatureImage = c.FeatureImage,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}

// --- Response DTOs ---

public sealed class CollectionsEnvelope
{
    [JsonPropertyName("collections")]
    public required List<CollectionResource> Collections { get; init; }

    [JsonPropertyName("meta")]
    public required CollectionsMeta Meta { get; init; }
}

public sealed class CollectionsMeta
{
    [JsonPropertyName("pagination")]
    public required CollectionsPagination Pagination { get; init; }
}

public sealed class CollectionsPagination
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

public sealed class CollectionResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = null!;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = null!;

    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filter { get; init; }

    [JsonPropertyName("feature_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FeatureImage { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? UpdatedAt { get; init; }

    [JsonPropertyName("posts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CollectionPostResource>? Posts { get; set; }
}

public sealed class CollectionPostResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = null!;

    [JsonPropertyName("feature_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FeatureImage { get; init; }

    [JsonPropertyName("published_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? PublishedAt { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
