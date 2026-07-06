using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

/// <summary>
/// Tests for IRecommendationService interface contract compliance.
/// Uses a fake in-memory implementation to validate business rules
/// without requiring a real database.
/// </summary>
public class RecommendationServiceTests
{
    private readonly FakeRecommendationService _sut = new();

    // ── CreateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesRecommendation()
    {
        var result = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example Site",
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("https://example.com", result.Url);
        Assert.Equal("Example Site", result.Title);
    }

    [Fact]
    public async Task CreateAsync_WithOptionalFields_SetsAllProperties()
    {
        var result = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example Site",
            Excerpt = "A great publication",
            Description = "Full description here",
            Favicon = "https://example.com/favicon.ico",
            FeaturedImage = "https://example.com/image.jpg",
            OneClickSubscribe = true,
        });

        Assert.Equal("A great publication", result.Excerpt);
        Assert.Equal("Full description here", result.Description);
        Assert.Equal("https://example.com/favicon.ico", result.Favicon);
        Assert.Equal("https://example.com/image.jpg", result.FeaturedImage);
        Assert.True(result.OneClickSubscribe);
    }

    [Fact]
    public async Task CreateAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var result = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        Assert.True(result.CreatedAt >= before);
        Assert.NotNull(result.UpdatedAt);
    }

    // ── GetAllAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Empty_ReturnsEmptyList()
    {
        var results = await _sut.GetAllAsync();
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByMostRecent()
    {
        await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://first.com",
            Title = "First",
        });
        await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://second.com",
            Title = "Second",
        });

        var results = await _sut.GetAllAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("Second", results[0].Title);
        Assert.Equal("First", results[1].Title);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsRecommendation()
    {
        var created = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        var found = await _sut.GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("non-existent-id");
        Assert.Null(result);
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesTitle()
    {
        var created = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Original",
        });

        await _sut.UpdateAsync(created.Id, new UpdateRecommendationRequest
        {
            Title = "Updated Title",
        });

        var updated = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("Updated Title", updated!.Title);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOneClickSubscribe()
    {
        var created = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
            OneClickSubscribe = false,
        });

        await _sut.UpdateAsync(created.Id, new UpdateRecommendationRequest
        {
            OneClickSubscribe = true,
        });

        var updated = await _sut.GetByIdAsync(created.Id);
        Assert.True(updated!.OneClickSubscribe);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateAsync("non-existent", new UpdateRecommendationRequest
            {
                Title = "Won't work",
            }));
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesRecommendation()
    {
        var created = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        await _sut.DeleteAsync(created.Id);

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        // Fake implementation is lenient; real one throws
        await _sut.DeleteAsync("non-existent-id");
    }

    // ── RecordClickEventAsync / GetClickCountAsync ──────────────

    [Fact]
    public async Task RecordClickEvent_IncrementsCount()
    {
        var rec = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        await _sut.RecordClickEventAsync(rec.Id, "member-123");

        var count = await _sut.GetClickCountAsync(rec.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordClickEvent_MultipleClicks_CountsAll()
    {
        var rec = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        await _sut.RecordClickEventAsync(rec.Id, "member-1");
        await _sut.RecordClickEventAsync(rec.Id, "member-2");
        await _sut.RecordClickEventAsync(rec.Id, null);

        var count = await _sut.GetClickCountAsync(rec.Id);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetClickCountAsync_NoClicks_ReturnsZero()
    {
        var count = await _sut.GetClickCountAsync("any-id");
        Assert.Equal(0, count);
    }

    // ── RecordSubscribeEventAsync / GetSubscribeCountAsync ──────

    [Fact]
    public async Task RecordSubscribeEvent_IncrementsCount()
    {
        var rec = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        await _sut.RecordSubscribeEventAsync(rec.Id, "member-123");

        var count = await _sut.GetSubscribeCountAsync(rec.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordSubscribeEvent_MultipleSubscribes_CountsAll()
    {
        var rec = await _sut.CreateAsync(new CreateRecommendationRequest
        {
            Url = "https://example.com",
            Title = "Example",
        });

        await _sut.RecordSubscribeEventAsync(rec.Id, "member-1");
        await _sut.RecordSubscribeEventAsync(rec.Id, "member-2");

        var count = await _sut.GetSubscribeCountAsync(rec.Id);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetSubscribeCountAsync_NoSubscribes_ReturnsZero()
    {
        var count = await _sut.GetSubscribeCountAsync("any-id");
        Assert.Equal(0, count);
    }

    // ════════════════════════════════════════════════════════════
    // Fake implementation
    // ════════════════════════════════════════════════════════════

    private sealed class FakeRecommendationService : IRecommendationService
    {
        private readonly List<Recommendation> _recommendations = [];
        private readonly List<RecommendationClickEvent> _clicks = [];
        private readonly List<RecommendationSubscribeEvent> _subscribes = [];

        public Task<List<Recommendation>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_recommendations
                .OrderByDescending(r => r.CreatedAt)
                .ToList());
        }

        public Task<Recommendation?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return Task.FromResult(_recommendations.FirstOrDefault(r => r.Id == id));
        }

        public Task<Recommendation> CreateAsync(
            CreateRecommendationRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var rec = new Recommendation
            {
                Id = Guid.NewGuid().ToString("D"),
                Url = request.Url,
                Title = request.Title,
                Excerpt = request.Excerpt,
                FeaturedImage = request.FeaturedImage,
                Favicon = request.Favicon,
                Description = request.Description,
                OneClickSubscribe = request.OneClickSubscribe,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _recommendations.Add(rec);
            return Task.FromResult(rec);
        }

        public Task UpdateAsync(
            string id, UpdateRecommendationRequest request, CancellationToken ct = default)
        {
            var rec = _recommendations.FirstOrDefault(r => r.Id == id)
                ?? throw new InvalidOperationException($"Recommendation {id} not found");

            if (!string.IsNullOrWhiteSpace(request.Title))
                rec.Title = request.Title;
            if (!string.IsNullOrWhiteSpace(request.Url))
                rec.Url = request.Url;
            if (request.Excerpt is not null)
                rec.Excerpt = request.Excerpt;
            if (request.FeaturedImage is not null)
                rec.FeaturedImage = request.FeaturedImage;
            if (request.Favicon is not null)
                rec.Favicon = request.Favicon;
            if (request.Description is not null)
                rec.Description = request.Description;
            if (request.OneClickSubscribe.HasValue)
                rec.OneClickSubscribe = request.OneClickSubscribe.Value;

            rec.UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var rec = _recommendations.FirstOrDefault(r => r.Id == id);
            if (rec is not null)
                _recommendations.Remove(rec);
            return Task.CompletedTask;
        }

        public Task RecordClickEventAsync(
            string recommendationId, string? memberId, CancellationToken ct = default)
        {
            _clicks.Add(new RecommendationClickEvent
            {
                Id = Guid.NewGuid().ToString("D"),
                RecommendationId = recommendationId,
                MemberId = memberId,
                CreatedAt = DateTime.UtcNow,
            });
            return Task.CompletedTask;
        }

        public Task RecordSubscribeEventAsync(
            string recommendationId, string? memberId, CancellationToken ct = default)
        {
            _subscribes.Add(new RecommendationSubscribeEvent
            {
                Id = Guid.NewGuid().ToString("D"),
                RecommendationId = recommendationId,
                MemberId = memberId,
                CreatedAt = DateTime.UtcNow,
            });
            return Task.CompletedTask;
        }

        public Task<int> GetClickCountAsync(string recommendationId, CancellationToken ct = default)
        {
            return Task.FromResult(_clicks.Count(c => c.RecommendationId == recommendationId));
        }

        public Task<int> GetSubscribeCountAsync(string recommendationId, CancellationToken ct = default)
        {
            return Task.FromResult(_subscribes.Count(s => s.RecommendationId == recommendationId));
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
