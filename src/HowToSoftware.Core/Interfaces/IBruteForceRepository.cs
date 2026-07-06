using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IBruteForceRepository
{
    Task<Brute?> GetAsync(string key, CancellationToken ct = default);
    Task UpsertAsync(Brute record, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
