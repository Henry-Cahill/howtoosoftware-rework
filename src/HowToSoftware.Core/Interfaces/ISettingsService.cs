using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ISettingsService
{
    // Typed accessors
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);
    Task<bool?> GetBoolAsync(string key, CancellationToken ct = default);
    Task<int?> GetIntAsync(string key, CancellationToken ct = default);
    Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default) where T : class;

    // CRUD
    Task<Setting?> GetAsync(string key, CancellationToken ct = default);
    Task<List<Setting>> GetAllAsync(CancellationToken ct = default);
    Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default);
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task SetBoolAsync(string key, bool value, CancellationToken ct = default);
    Task SetIntAsync(string key, int value, CancellationToken ct = default);
    Task SetJsonAsync<T>(string key, T value, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string key, CancellationToken ct = default);

    // Cache management
    void InvalidateCache();
    void InvalidateCache(string key);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
