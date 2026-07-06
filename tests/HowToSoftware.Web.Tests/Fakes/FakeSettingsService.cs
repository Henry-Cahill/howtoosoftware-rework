using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Tests.Fakes;

internal class FakeSettingsService : ISettingsService
{
    public Dictionary<string, string?> Settings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<string?> GetStringAsync(string key, CancellationToken ct = default)
        => Task.FromResult(Settings.GetValueOrDefault(key));

    public Task<bool?> GetBoolAsync(string key, CancellationToken ct = default)
    {
        if (Settings.TryGetValue(key, out var v) && bool.TryParse(v, out var b))
            return Task.FromResult<bool?>(b);
        return Task.FromResult<bool?>(null);
    }

    public Task<int?> GetIntAsync(string key, CancellationToken ct = default)
    {
        if (Settings.TryGetValue(key, out var v) && int.TryParse(v, out var i))
            return Task.FromResult<int?>(i);
        return Task.FromResult<int?>(null);
    }

    public Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(default);

    public Task<Setting?> GetAsync(string key, CancellationToken ct = default)
    {
        if (Settings.TryGetValue(key, out var v))
            return Task.FromResult<Setting?>(new Setting { Id = key, Key = key, Value = v, Type = "string", CreatedAt = DateTime.UtcNow });
        return Task.FromResult<Setting?>(null);
    }

    public Task<List<Setting>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Settings.Select(kv => new Setting
        {
            Id = kv.Key, Key = kv.Key, Value = kv.Value, Type = "string", CreatedAt = DateTime.UtcNow
        }).ToList());

    public Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default)
        => Task.FromResult<List<Setting>>([]);

    public Task SetAsync(string key, string? value, CancellationToken ct = default)
    { Settings[key] = value; return Task.CompletedTask; }

    public Task SetBoolAsync(string key, bool value, CancellationToken ct = default)
    { Settings[key] = value.ToString(); return Task.CompletedTask; }

    public Task SetIntAsync(string key, int value, CancellationToken ct = default)
    { Settings[key] = value.ToString(); return Task.CompletedTask; }

    public Task SetJsonAsync<T>(string key, T value, CancellationToken ct = default) where T : class
    { Settings[key] = System.Text.Json.JsonSerializer.Serialize(value); return Task.CompletedTask; }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    { Settings.Remove(key); return Task.CompletedTask; }

    public void InvalidateCache() { }
    public void InvalidateCache(string key) { }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
