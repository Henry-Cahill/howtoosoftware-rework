using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class InviteRepository(AppDbContext db) : IInviteRepository
{
    public async Task<Invite?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Invites
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<Invite?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await db.Invites
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Email == email, ct);
    }

    public async Task<Invite?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await db.Invites
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Token == token, ct);
    }

    public async Task<List<Invite>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Invites
            .Include(i => i.Role)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Invite invite, CancellationToken ct = default)
    {
        db.Invites.Add(invite);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invite invite, CancellationToken ct = default)
    {
        db.Invites.Update(invite);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Invites.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
