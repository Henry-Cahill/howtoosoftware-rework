using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class MemberRepository(AppDbContext db) : IMemberRepository
{
    public async Task<Member?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await MembersWithIncludes()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await MembersWithIncludes()
            .FirstOrDefaultAsync(m => m.Email == email, ct);
    }

    public async Task<Member?> GetByUuidAsync(string uuid, CancellationToken ct = default)
    {
        return await MembersWithIncludes()
            .FirstOrDefaultAsync(m => m.Uuid == uuid, ct);
    }

    public async Task<PagedResult<Member>> GetAllAsync(string? status, string? search, string? labelId, int page, int pageSize, MemberEngagementFilter? engagement = null, CancellationToken ct = default)
    {
        var query = MembersWithIncludes().AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(m => m.Status == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(m => m.Email.Contains(search)
                || (m.Name != null && m.Name.Contains(search)));

        if (!string.IsNullOrEmpty(labelId))
            query = query.Where(m => m.MembersLabels.Any(ml => ml.LabelId == labelId));

        query = ApplyEngagementFilter(query, engagement);

        query = query.OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Member>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<Member>> GetAllForExportAsync(string? status, string? labelId, MemberEngagementFilter? engagement = null, CancellationToken ct = default)
    {
        var query = MembersWithIncludes().AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(m => m.Status == status);

        if (!string.IsNullOrEmpty(labelId))
            query = query.Where(m => m.MembersLabels.Any(ml => ml.LabelId == labelId));

        query = ApplyEngagementFilter(query, engagement);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
    }

    private static IQueryable<Member> ApplyEngagementFilter(IQueryable<Member> query, MemberEngagementFilter? engagement)
    {
        if (engagement is null) return query;

        var now = DateTime.UtcNow;
        return engagement switch
        {
            MemberEngagementFilter.ActiveLast30Days =>
                query.Where(m => m.LastSeenAt != null && m.LastSeenAt >= now.AddDays(-30)),
            MemberEngagementFilter.NeverOpenedEmail =>
                query.Where(m => m.EmailCount > 0 && m.EmailOpenedCount == 0),
            // EmailOpenedCount * 2 > EmailCount  ⇔  open rate > 50%
            MemberEngagementFilter.OpenedOverHalf =>
                query.Where(m => m.EmailCount > 0 && m.EmailOpenedCount * 2 > m.EmailCount),
            MemberEngagementFilter.InactiveLast90Days =>
                query.Where(m => m.LastSeenAt == null || m.LastSeenAt < now.AddDays(-90)),
            _ => query,
        };
    }

    public async Task<List<Member>> GetByLabelAsync(string labelId, CancellationToken ct = default)
    {
        return await db.MembersLabels
            .Where(ml => ml.LabelId == labelId)
            .Select(ml => ml.Member)
            .OrderBy(m => m.Email)
            .ToListAsync(ct);
    }

    public async Task<List<Member>> GetNewsletterSubscribersAsync(string newsletterId, CancellationToken ct = default)
    {
        return await db.MembersNewsletters
            .Where(mn => mn.NewsletterId == newsletterId && !mn.Member.EmailDisabled)
            .Select(mn => mn.Member)
            .OrderBy(m => m.Email)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Member member, CancellationToken ct = default)
    {
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Member member, CancellationToken ct = default)
    {
        db.Members.Update(member);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Members.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<int> GetCountAsync(string? status, CancellationToken ct = default)
    {
        var query = db.Members.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(m => m.Status == status);

        return await query.CountAsync(ct);
    }

    public async Task AddLabelToMemberAsync(string memberId, string labelId, CancellationToken ct = default)
    {
        var exists = await db.MembersLabels
            .AnyAsync(ml => ml.MemberId == memberId && ml.LabelId == labelId, ct);
        if (exists) return;

        var maxSort = await db.MembersLabels
            .Where(ml => ml.MemberId == memberId)
            .Select(ml => (int?)ml.SortOrder)
            .MaxAsync(ct) ?? -1;

        db.MembersLabels.Add(new MembersLabel
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = memberId,
            LabelId = labelId,
            SortOrder = maxSort + 1,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveLabelFromMemberAsync(string memberId, string labelId, CancellationToken ct = default)
    {
        await db.MembersLabels
            .Where(ml => ml.MemberId == memberId && ml.LabelId == labelId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task UpdateNoteAsync(string memberId, string? note, CancellationToken ct = default)
    {
        await db.Members
            .Where(m => m.Id == memberId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Note, note)
                .SetProperty(m => m.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task<bool> LinkStripeCustomerAsync(string memberId, string stripeCustomerId, string? name, string? email, CancellationToken ct = default)
    {
        // Skip if this customer id is already linked anywhere.
        var existing = await db.MembersStripeCustomers
            .AnyAsync(c => c.CustomerId == stripeCustomerId, ct);
        if (existing) return false;

        db.MembersStripeCustomers.Add(new MembersStripeCustomer
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = memberId,
            CustomerId = stripeCustomerId,
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddCreatedEventAsync(MembersCreatedEvent evt, CancellationToken ct = default)
    {
        db.Set<MembersCreatedEvent>().Add(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddStatusEventAsync(MembersStatusEvent evt, CancellationToken ct = default)
    {
        db.Set<MembersStatusEvent>().Add(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddSubscribeEventAsync(MembersSubscribeEvent evt, CancellationToken ct = default)
    {
        db.Set<MembersSubscribeEvent>().Add(evt);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<Member> MembersWithIncludes()
    {
        return db.Members
            .Include(m => m.MembersLabels)
                .ThenInclude(ml => ml.Label)
            .Include(m => m.MembersNewsletters)
                .ThenInclude(mn => mn.Newsletter)
            .Include(m => m.MembersProducts)
            .AsSplitQuery();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
