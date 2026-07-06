using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class BruteForceServiceTests
{
    private readonly FakeBruteForceRepo _repo = new();
    private readonly BruteForceService _sut;

    public BruteForceServiceTests()
    {
        _sut = new BruteForceService(_repo);
    }

    // ── TrackAsync — first attempt ─────────────────────────────

    [Fact]
    public async Task TrackAsync_FirstAttempt_ReturnsNotBlocked()
    {
        var blocked = await _sut.TrackAsync("test-key", 5, TimeSpan.FromMinutes(10));

        Assert.False(blocked);
        Assert.Single(_repo.Records);
        Assert.Equal(1, _repo.Records["test-key"].Count);
    }

    // ── TrackAsync — within limits ─────────────────────────────

    [Fact]
    public async Task TrackAsync_UnderLimit_ReturnsNotBlocked()
    {
        for (var i = 0; i < 5; i++)
        {
            var blocked = await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));
            Assert.False(blocked);
        }

        Assert.Equal(5, _repo.Records["key"].Count);
    }

    // ── TrackAsync — exceeds limit ─────────────────────────────

    [Fact]
    public async Task TrackAsync_ExceedsLimit_ReturnsBlocked()
    {
        // First 5 should pass
        for (var i = 0; i < 5; i++)
            await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));

        // 6th should be blocked
        var blocked = await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));

        Assert.True(blocked);
        Assert.Equal(6, _repo.Records["key"].Count);
    }

    // ── TrackAsync — different keys are independent ────────────

    [Fact]
    public async Task TrackAsync_DifferentKeys_AreIndependent()
    {
        for (var i = 0; i < 6; i++)
            await _sut.TrackAsync("key-a", 5, TimeSpan.FromMinutes(10));

        var blocked = await _sut.TrackAsync("key-b", 5, TimeSpan.FromMinutes(10));

        Assert.False(blocked);
    }

    // ── TrackAsync — window expiry ─────────────────────────────

    [Fact]
    public async Task TrackAsync_ExpiredWindow_ResetsCounter()
    {
        // Fill up the counter
        for (var i = 0; i < 5; i++)
            await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));

        // Simulate window expiry by backdating the record
        var record = _repo.Records["key"];
        record.FirstRequest = DateTimeOffset.UtcNow.AddMinutes(-11).ToUnixTimeMilliseconds();

        // Next attempt should reset and succeed
        var blocked = await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));

        Assert.False(blocked);
        Assert.Equal(1, _repo.Records["key"].Count);
    }

    // ── TrackAsync — stays blocked until window expires ────────

    [Fact]
    public async Task TrackAsync_StaysBlockedUntilWindowExpires()
    {
        for (var i = 0; i < 6; i++)
            await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));

        // Still blocked
        var blocked = await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));
        Assert.True(blocked);
    }

    // ── ResetAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ResetAsync_RemovesRecord()
    {
        await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));
        Assert.Single(_repo.Records);

        await _sut.ResetAsync("key");

        Assert.Empty(_repo.Records);
    }

    [Fact]
    public async Task ResetAsync_AllowsAttemptsAgainAfterReset()
    {
        // Block the key
        for (var i = 0; i < 6; i++)
            await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));

        Assert.True(await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10)));

        // Reset
        await _sut.ResetAsync("key");

        // Should be allowed again
        var blocked = await _sut.TrackAsync("key", 5, TimeSpan.FromMinutes(10));
        Assert.False(blocked);
        Assert.Equal(1, _repo.Records["key"].Count);
    }

    [Fact]
    public async Task ResetAsync_NonexistentKey_DoesNotThrow()
    {
        await _sut.ResetAsync("nonexistent");
    }

    // ── Fake ───────────────────────────────────────────────────

    private class FakeBruteForceRepo : IBruteForceRepository
    {
        public Dictionary<string, Brute> Records { get; } = new();

        public Task<Brute?> GetAsync(string key, CancellationToken ct = default)
        {
            Records.TryGetValue(key, out var record);
            return Task.FromResult(record);
        }

        public Task UpsertAsync(Brute record, CancellationToken ct = default)
        {
            Records[record.Key] = record;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            Records.Remove(key);
            return Task.CompletedTask;
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
