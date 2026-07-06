using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class SettingsRepository(AppDbContext db) : ISettingsRepository
{
    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await db.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);
    }

    public async Task<List<Setting>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Settings
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Key)
            .ToListAsync(ct);
    }

    public async Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default)
    {
        return await db.Settings
            .Where(s => s.Group == group)
            .OrderBy(s => s.Key)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(string key, string? value, CancellationToken ct = default)
    {
        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);

        if (setting is not null)
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Settings.Add(new Setting
            {
                Id = Guid.NewGuid().ToString("N"),
                Key = key,
                Value = value,
                Type = "string",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await db.Settings.Where(s => s.Key == key).ExecuteDeleteAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
