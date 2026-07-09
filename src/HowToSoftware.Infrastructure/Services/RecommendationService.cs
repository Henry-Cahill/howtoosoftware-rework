using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public sealed class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        AppDbContext db,
        ILogger<RecommendationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ================================================================
    // Queries
    // ================================================================

    public async Task<List<Recommendation>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Recommendations
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Recommendation?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Recommendations
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    // ================================================================
    // Create
    // ================================================================

    public async Task<Recommendation> CreateAsync(
        CreateRecommendationRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var recommendation = new Recommendation
        {
            Id = ObjectIdGenerator.New(),
            Url = request.Url.Trim(),
            Title = request.Title.Trim(),
            Excerpt = request.Excerpt?.Trim(),
            FeaturedImage = request.FeaturedImage?.Trim(),
            Favicon = request.Favicon?.Trim(),
            Description = request.Description?.Trim(),
            OneClickSubscribe = request.OneClickSubscribe,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Recommendations.Add(recommendation);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created recommendation {RecommendationId}: {Title}",
            recommendation.Id, recommendation.Title);

        return recommendation;
    }

    // ================================================================
    // Update
    // ================================================================

    public async Task UpdateAsync(
        string id, UpdateRecommendationRequest request, CancellationToken ct = default)
    {
        var recommendation = await _db.Recommendations.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Recommendation {id} not found");

        if (!string.IsNullOrWhiteSpace(request.Title))
            recommendation.Title = request.Title.Trim();
        if (!string.IsNullOrWhiteSpace(request.Url))
            recommendation.Url = request.Url.Trim();
        if (request.Excerpt is not null)
            recommendation.Excerpt = request.Excerpt.Trim();
        if (request.FeaturedImage is not null)
            recommendation.FeaturedImage = request.FeaturedImage.Trim();
        if (request.Favicon is not null)
            recommendation.Favicon = request.Favicon.Trim();
        if (request.Description is not null)
            recommendation.Description = request.Description.Trim();
        if (request.OneClickSubscribe.HasValue)
            recommendation.OneClickSubscribe = request.OneClickSubscribe.Value;

        recommendation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated recommendation {RecommendationId}", LogSanitizer.SanitizeForLog(id));
    }

    // ================================================================
    // Delete
    // ================================================================

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var recommendation = await _db.Recommendations.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Recommendation {id} not found");

        _db.Recommendations.Remove(recommendation);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted recommendation {RecommendationId}", LogSanitizer.SanitizeForLog(id));
    }

    // ================================================================
    // Event Recording
    // ================================================================

    public async Task RecordClickEventAsync(
        string recommendationId, string? memberId, CancellationToken ct = default)
    {
        var click = new RecommendationClickEvent
        {
            Id = ObjectIdGenerator.New(),
            RecommendationId = recommendationId,
            MemberId = memberId,
            CreatedAt = DateTime.UtcNow,
        };

        _db.RecommendationClickEvents.Add(click);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordSubscribeEventAsync(
        string recommendationId, string? memberId, CancellationToken ct = default)
    {
        var subscribe = new RecommendationSubscribeEvent
        {
            Id = ObjectIdGenerator.New(),
            RecommendationId = recommendationId,
            MemberId = memberId,
            CreatedAt = DateTime.UtcNow,
        };

        _db.RecommendationSubscribeEvents.Add(subscribe);
        await _db.SaveChangesAsync(ct);
    }

    // ================================================================
    // Analytics
    // ================================================================

    public async Task<int> GetClickCountAsync(string recommendationId, CancellationToken ct = default)
    {
        return await _db.RecommendationClickEvents
            .CountAsync(c => c.RecommendationId == recommendationId, ct);
    }

    public async Task<int> GetSubscribeCountAsync(string recommendationId, CancellationToken ct = default)
    {
        return await _db.RecommendationSubscribeEvents
            .CountAsync(s => s.RecommendationId == recommendationId, ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
