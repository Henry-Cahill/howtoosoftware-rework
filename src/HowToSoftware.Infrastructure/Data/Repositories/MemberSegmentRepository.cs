using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class MemberSegmentRepository(AppDbContext db) : IMemberSegmentRepository
{
    public async Task<List<MemberSegment>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.MemberSegments
            .Include(s => s.Label)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<MemberSegment?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.MemberSegments
            .Include(s => s.Label)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task AddAsync(MemberSegment segment, CancellationToken ct = default)
    {
        db.MemberSegments.Add(segment);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MemberSegment segment, CancellationToken ct = default)
    {
        segment.UpdatedAt = DateTime.UtcNow;
        db.MemberSegments.Update(segment);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.MemberSegments.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
