using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ISettingsRepository
{
    Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<List<Setting>> GetAllAsync(CancellationToken ct = default);
    Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default);
    Task UpsertAsync(string key, string? value, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
