using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class LabelRepository(AppDbContext db) : ILabelRepository
{
    public async Task<List<Label>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Labels
            .OrderBy(l => l.Name)
            .ToListAsync(ct);
    }

    public async Task<Label?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Labels.FindAsync([id], ct);
    }

    public async Task<Label?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.Labels
            .FirstOrDefaultAsync(l => l.Slug == slug, ct);
    }

    public async Task AddAsync(Label label, CancellationToken ct = default)
    {
        db.Labels.Add(label);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Label label, CancellationToken ct = default)
    {
        db.Labels.Update(label);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Labels.Where(l => l.Id == id).ExecuteDeleteAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
