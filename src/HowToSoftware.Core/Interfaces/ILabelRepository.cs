using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ILabelRepository
{
    Task<List<Label>> GetAllAsync(CancellationToken ct = default);
    Task<Label?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Label?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Label label, CancellationToken ct = default);
    Task UpdateAsync(Label label, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
