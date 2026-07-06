using System.Collections.Concurrent;
using System.Text.Json;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

public class SettingsService(ISettingsRepository settingsRepository) : ISettingsService
{
    private readonly ConcurrentDictionary<string, Setting?> _cache = new();
    private List<Setting>? _allCache;
    private readonly object _allCacheLock = new();

    // ── Typed accessors ─────────────────────────────────────────

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        var setting = await GetCachedAsync(key, ct);
        return setting?.Value;
    }

    public async Task<bool?> GetBoolAsync(string key, CancellationToken ct = default)
    {
        var setting = await GetCachedAsync(key, ct);
        if (setting?.Value is null) return null;

        return setting.Value switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => bool.TryParse(setting.Value, out var result) ? result : null,
        };
    }

    public async Task<int?> GetIntAsync(string key, CancellationToken ct = default)
    {
        var setting = await GetCachedAsync(key, ct);
        if (setting?.Value is null) return null;

        return int.TryParse(setting.Value, out var result) ? result : null;
    }

    public async Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var setting = await GetCachedAsync(key, ct);
        if (setting?.Value is null) return null;

        return JsonSerializer.Deserialize<T>(setting.Value);
    }

    // ── CRUD ────────────────────────────────────────────────────

    public async Task<Setting?> GetAsync(string key, CancellationToken ct = default)
    {
        return await GetCachedAsync(key, ct);
    }

    public async Task<List<Setting>> GetAllAsync(CancellationToken ct = default)
    {
        lock (_allCacheLock)
        {
            if (_allCache is not null)
                return _allCache;
        }

        var all = await settingsRepository.GetAllAsync(ct);

        lock (_allCacheLock)
        {
            _allCache = all;
        }

        // Populate individual key cache
        foreach (var s in all)
            _cache[s.Key] = s;

        return all;
    }

    public async Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default)
    {
        return await settingsRepository.GetByGroupAsync(group, ct);
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        await settingsRepository.UpsertAsync(key, value, ct);
        InvalidateCache(key);
    }

    public async Task SetBoolAsync(string key, bool value, CancellationToken ct = default)
    {
        await SetAsync(key, value ? "true" : "false", ct);
    }

    public async Task SetIntAsync(string key, int value, CancellationToken ct = default)
    {
        await SetAsync(key, value.ToString(), ct);
    }

    public async Task SetJsonAsync<T>(string key, T value, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await SetAsync(key, json, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await settingsRepository.DeleteAsync(key, ct);
        InvalidateCache(key);
    }

    // ── Cache management ────────────────────────────────────────

    public void InvalidateCache()
    {
        _cache.Clear();
        lock (_allCacheLock)
        {
            _allCache = null;
        }
    }

    public void InvalidateCache(string key)
    {
        _cache.TryRemove(key, out _);
        lock (_allCacheLock)
        {
            _allCache = null;
        }
    }

    // ── Private ─────────────────────────────────────────────────

    private async Task<Setting?> GetCachedAsync(string key, CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var setting = await settingsRepository.GetByKeyAsync(key, ct);
        _cache[key] = setting;
        return setting;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
