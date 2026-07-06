using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class BruteForceRepository(AppDbContext db) : IBruteForceRepository
{
    public async Task<Brute?> GetAsync(string key, CancellationToken ct = default)
    {
        return await db.Brute.FindAsync([key], ct);
    }

    public async Task UpsertAsync(Brute record, CancellationToken ct = default)
    {
        var existing = await db.Brute.FindAsync([record.Key], ct);

        if (existing is not null)
        {
            existing.FirstRequest = record.FirstRequest;
            existing.LastRequest = record.LastRequest;
            existing.Lifetime = record.Lifetime;
            existing.Count = record.Count;
        }
        else
        {
            db.Brute.Add(record);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var record = await db.Brute.FindAsync([key], ct);
        if (record is not null)
        {
            db.Brute.Remove(record);
            await db.SaveChangesAsync(ct);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
