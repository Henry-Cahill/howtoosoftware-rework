using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IInviteRepository
{
    Task<Invite?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Invite?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Invite?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<List<Invite>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Invite invite, CancellationToken ct = default);
    Task UpdateAsync(Invite invite, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
