using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IMemberSegmentRepository
{
    Task<List<MemberSegment>> GetAllAsync(CancellationToken ct = default);
    Task<MemberSegment?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(MemberSegment segment, CancellationToken ct = default);
    Task UpdateAsync(MemberSegment segment, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
