using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class SettingsServiceTests
{
    private readonly FakeSettingsRepository _repo = new();
    private readonly SettingsService _sut;

    public SettingsServiceTests()
    {
        _sut = new SettingsService(_repo);
    }

    // ── GetStringAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetStringAsync_ExistingKey_ReturnsValue()
    {
        _repo.Seed("site_title", "HowToSoftware");

        var result = await _sut.GetStringAsync("site_title");

        Assert.Equal("HowToSoftware", result);
    }

    [Fact]
    public async Task GetStringAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.GetStringAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStringAsync_CachesResult()
    {
        _repo.Seed("key", "value");

        await _sut.GetStringAsync("key");
        await _sut.GetStringAsync("key");

        Assert.Equal(1, _repo.GetByKeyCallCount);
    }

    [Fact]
    public async Task GetStringAsync_CachesNullResult()
    {
        await _sut.GetStringAsync("missing");
        await _sut.GetStringAsync("missing");

        Assert.Equal(1, _repo.GetByKeyCallCount);
    }

    // ── GetBoolAsync ────────────────────────────────────────────

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public async Task GetBoolAsync_ParsesValues(string stored, bool expected)
    {
        _repo.Seed("flag", stored);

        var result = await _sut.GetBoolAsync("flag");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetBoolAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.GetBoolAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBoolAsync_NullValue_ReturnsNull()
    {
        _repo.Seed("flag", null);

        var result = await _sut.GetBoolAsync("flag");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBoolAsync_InvalidValue_ReturnsNull()
    {
        _repo.Seed("flag", "not-a-bool");

        var result = await _sut.GetBoolAsync("flag");

        Assert.Null(result);
    }

    // ── GetIntAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetIntAsync_ValidInt_ReturnsValue()
    {
        _repo.Seed("count", "42");

        var result = await _sut.GetIntAsync("count");

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetIntAsync_NegativeInt_ReturnsValue()
    {
        _repo.Seed("offset", "-5");

        var result = await _sut.GetIntAsync("offset");

        Assert.Equal(-5, result);
    }

    [Fact]
    public async Task GetIntAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.GetIntAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetIntAsync_InvalidValue_ReturnsNull()
    {
        _repo.Seed("count", "abc");

        var result = await _sut.GetIntAsync("count");

        Assert.Null(result);
    }

    // ── GetJsonAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetJsonAsync_ValidJson_Deserializes()
    {
        _repo.Seed("nav", """[{"Label":"Home","Url":"/"}]""");

        var result = await _sut.GetJsonAsync<List<NavItem>>("nav");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Home", result[0].Label);
        Assert.Equal("/", result[0].Url);
    }

    [Fact]
    public async Task GetJsonAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.GetJsonAsync<List<NavItem>>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetJsonAsync_NullValue_ReturnsNull()
    {
        _repo.Seed("nav", null);

        var result = await _sut.GetJsonAsync<List<NavItem>>("nav");

        Assert.Null(result);
    }

    // ── GetAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsSetting()
    {
        _repo.Seed("title", "Test");

        var result = await _sut.GetAsync("title");

        Assert.NotNull(result);
        Assert.Equal("title", result.Key);
        Assert.Equal("Test", result.Value);
    }

    // ── GetAllAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        _repo.Seed("a", "1");
        _repo.Seed("b", "2");

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_CachesResult()
    {
        _repo.Seed("a", "1");

        await _sut.GetAllAsync();
        await _sut.GetAllAsync();

        Assert.Equal(1, _repo.GetAllCallCount);
    }

    [Fact]
    public async Task GetAllAsync_PopulatesKeyCache()
    {
        _repo.Seed("x", "val");

        await _sut.GetAllAsync();
        await _sut.GetStringAsync("x");

        // GetByKey should not be called since GetAll populated the cache
        Assert.Equal(0, _repo.GetByKeyCallCount);
    }

    // ── GetByGroupAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetByGroupAsync_FiltersCorrectly()
    {
        _repo.Seed("a", "1", "core");
        _repo.Seed("b", "2", "site");

        var result = await _sut.GetByGroupAsync("core");

        Assert.Single(result);
        Assert.Equal("a", result[0].Key);
    }

    // ── SetAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_NewKey_Creates()
    {
        await _sut.SetAsync("new_key", "new_value");

        var setting = _repo.Settings.First(s => s.Key == "new_key");
        Assert.Equal("new_value", setting.Value);
    }

    [Fact]
    public async Task SetAsync_ExistingKey_Updates()
    {
        _repo.Seed("key", "old");

        await _sut.SetAsync("key", "new");

        var setting = _repo.Settings.First(s => s.Key == "key");
        Assert.Equal("new", setting.Value);
    }

    [Fact]
    public async Task SetAsync_InvalidatesCache()
    {
        _repo.Seed("key", "old");
        await _sut.GetStringAsync("key"); // populate cache

        await _sut.SetAsync("key", "new");
        var result = await _sut.GetStringAsync("key");

        Assert.Equal("new", result);
        Assert.Equal(2, _repo.GetByKeyCallCount); // re-fetched after invalidation
    }

    // ── SetBoolAsync ────────────────────────────────────────────

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public async Task SetBoolAsync_StoresCorrectString(bool input, string expected)
    {
        await _sut.SetBoolAsync("flag", input);

        var setting = _repo.Settings.First(s => s.Key == "flag");
        Assert.Equal(expected, setting.Value);
    }

    // ── SetIntAsync ─────────────────────────────────────────────

    [Fact]
    public async Task SetIntAsync_StoresAsString()
    {
        await _sut.SetIntAsync("count", 99);

        var setting = _repo.Settings.First(s => s.Key == "count");
        Assert.Equal("99", setting.Value);
    }

    // ── SetJsonAsync ────────────────────────────────────────────

    [Fact]
    public async Task SetJsonAsync_SerializesObject()
    {
        var nav = new List<NavItem> { new() { Label = "Home", Url = "/" } };

        await _sut.SetJsonAsync("nav", nav);

        var setting = _repo.Settings.First(s => s.Key == "nav");
        Assert.Contains("Home", setting.Value);
        Assert.Contains("/", setting.Value);
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesSetting()
    {
        _repo.Seed("key", "val");

        await _sut.DeleteAsync("key");

        Assert.DoesNotContain(_repo.Settings, s => s.Key == "key");
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache()
    {
        _repo.Seed("key", "val");
        await _sut.GetStringAsync("key"); // populate cache

        await _sut.DeleteAsync("key");
        var result = await _sut.GetStringAsync("key");

        Assert.Null(result);
    }

    // ── InvalidateCache ─────────────────────────────────────────

    [Fact]
    public async Task InvalidateCache_ForcesRefetch()
    {
        _repo.Seed("key", "val");
        await _sut.GetStringAsync("key"); // cache miss → fetch
        Assert.Equal(1, _repo.GetByKeyCallCount);

        _sut.InvalidateCache();
        await _sut.GetStringAsync("key"); // should re-fetch

        Assert.Equal(2, _repo.GetByKeyCallCount);
    }

    [Fact]
    public async Task InvalidateCache_ByKey_OnlyAffectsThatKey()
    {
        _repo.Seed("a", "1");
        _repo.Seed("b", "2");
        await _sut.GetStringAsync("a");
        await _sut.GetStringAsync("b");
        Assert.Equal(2, _repo.GetByKeyCallCount);

        _sut.InvalidateCache("a");
        await _sut.GetStringAsync("a"); // re-fetched
        await _sut.GetStringAsync("b"); // still cached

        Assert.Equal(3, _repo.GetByKeyCallCount);
    }

    [Fact]
    public async Task InvalidateCache_AllCache_IsAlsoCleared()
    {
        _repo.Seed("a", "1");
        await _sut.GetAllAsync();
        Assert.Equal(1, _repo.GetAllCallCount);

        _sut.InvalidateCache();
        await _sut.GetAllAsync();

        Assert.Equal(2, _repo.GetAllCallCount);
    }

    // ── Test helpers ────────────────────────────────────────────

    private sealed class NavItem
    {
        public string Label { get; set; } = "";
        public string Url { get; set; } = "";
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public List<Setting> Settings { get; } = [];
        public int GetByKeyCallCount { get; private set; }
        public int GetAllCallCount { get; private set; }

        public void Seed(string key, string? value, string group = "core")
        {
            Settings.Add(new Setting
            {
                Id = Guid.NewGuid().ToString("N"),
                Group = group,
                Key = key,
                Value = value,
                Type = "string",
                CreatedAt = DateTime.UtcNow,
            });
        }

        public Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            GetByKeyCallCount++;
            return Task.FromResult(Settings.FirstOrDefault(s => s.Key == key));
        }

        public Task<List<Setting>> GetAllAsync(CancellationToken ct = default)
        {
            GetAllCallCount++;
            return Task.FromResult(Settings.OrderBy(s => s.Group).ThenBy(s => s.Key).ToList());
        }

        public Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default)
        {
            return Task.FromResult(Settings.Where(s => s.Group == group).OrderBy(s => s.Key).ToList());
        }

        public Task UpsertAsync(string key, string? value, CancellationToken ct = default)
        {
            var existing = Settings.FirstOrDefault(s => s.Key == key);
            if (existing is not null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                Settings.Add(new Setting
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Key = key,
                    Value = value,
                    Type = "string",
                    CreatedAt = DateTime.UtcNow,
                });
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            Settings.RemoveAll(s => s.Key == key);
            return Task.CompletedTask;
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
