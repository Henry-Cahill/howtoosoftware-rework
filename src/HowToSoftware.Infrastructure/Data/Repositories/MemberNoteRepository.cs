using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class MemberNoteRepository(AppDbContext db) : IMemberNoteRepository
{
    public async Task<List<MemberNote>> GetByMemberAsync(string memberId, CancellationToken ct = default)
    {
        return await db.MemberNotes
            .Where(n => n.MemberId == memberId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(MemberNote note, CancellationToken ct = default)
    {
        db.MemberNotes.Add(note);
        await db.SaveChangesAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
