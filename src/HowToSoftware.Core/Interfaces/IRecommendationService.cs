using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IRecommendationService
{
    /// <summary>Gets all recommendations, most recent first.</summary>
    Task<List<Recommendation>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Gets a single recommendation by ID.</summary>
    Task<Recommendation?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Creates a new recommendation.</summary>
    Task<Recommendation> CreateAsync(CreateRecommendationRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing recommendation.</summary>
    Task UpdateAsync(string id, UpdateRecommendationRequest request, CancellationToken ct = default);

    /// <summary>Deletes a recommendation and its associated events.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>Records a click event for a recommendation.</summary>
    Task RecordClickEventAsync(string recommendationId, string? memberId, CancellationToken ct = default);

    /// <summary>Records a subscribe event for a recommendation.</summary>
    Task RecordSubscribeEventAsync(string recommendationId, string? memberId, CancellationToken ct = default);

    /// <summary>Gets the number of click events for a recommendation.</summary>
    Task<int> GetClickCountAsync(string recommendationId, CancellationToken ct = default);

    /// <summary>Gets the number of subscribe events for a recommendation.</summary>
    Task<int> GetSubscribeCountAsync(string recommendationId, CancellationToken ct = default);
}

public record CreateRecommendationRequest
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public string? Excerpt { get; init; }
    public string? FeaturedImage { get; init; }
    public string? Favicon { get; init; }
    public string? Description { get; init; }
    public bool OneClickSubscribe { get; init; }
}

public record UpdateRecommendationRequest
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Excerpt { get; init; }
    public string? FeaturedImage { get; init; }
    public string? Favicon { get; init; }
    public string? Description { get; init; }
    public bool? OneClickSubscribe { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
