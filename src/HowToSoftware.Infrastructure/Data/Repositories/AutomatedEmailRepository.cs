using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class AutomatedEmailRepository(AppDbContext db) : IAutomatedEmailRepository
{
    public async Task<AutomatedEmail?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.AutomatedEmails.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<AutomatedEmail?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.AutomatedEmails.FirstOrDefaultAsync(e => e.Slug == slug, ct);
    }

    public async Task<List<AutomatedEmail>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.AutomatedEmails.OrderBy(e => e.Name).ToListAsync(ct);
    }

    public async Task AddAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
    {
        db.AutomatedEmails.Add(automatedEmail);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
    {
        db.AutomatedEmails.Update(automatedEmail);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.AutomatedEmails.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<bool> HasRecipientAsync(string automatedEmailId, string memberId, CancellationToken ct = default)
    {
        return await db.AutomatedEmailRecipients
            .AnyAsync(r => r.AutomatedEmailId == automatedEmailId && r.MemberId == memberId, ct);
    }

    public async Task AddRecipientAsync(AutomatedEmailRecipient recipient, CancellationToken ct = default)
    {
        db.AutomatedEmailRecipients.Add(recipient);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetRecipientCountAsync(string automatedEmailId, CancellationToken ct = default)
    {
        return await db.AutomatedEmailRecipients
            .CountAsync(r => r.AutomatedEmailId == automatedEmailId, ct);
    }

    public async Task<AutomatedEmailStatistics> GetStatisticsAsync(string automatedEmailId, CancellationToken ct = default)
    {
        var agg = await db.AutomatedEmailRecipients
            .Where(r => r.AutomatedEmailId == automatedEmailId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Sent = g.Count(),
                Delivered = g.Count(r => r.DeliveredAt != null),
                Opened = g.Count(r => r.OpenedAt != null),
                Clicked = g.Count(r => r.ClickedAt != null),
                Failed = g.Count(r => r.FailedAt != null),
                Bounced = g.Count(r => r.BouncedAt != null),
            })
            .FirstOrDefaultAsync(ct);

        return agg is null
            ? AutomatedEmailStatistics.Empty
            : new AutomatedEmailStatistics(agg.Sent, agg.Delivered, agg.Opened, agg.Clicked, agg.Failed, agg.Bounced);
    }

    public async Task<Dictionary<string, AutomatedEmailStatistics>> GetStatisticsBatchAsync(
        IEnumerable<string> automatedEmailIds, CancellationToken ct = default)
    {
        var ids = automatedEmailIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var rows = await db.AutomatedEmailRecipients
            .Where(r => ids.Contains(r.AutomatedEmailId))
            .GroupBy(r => r.AutomatedEmailId)
            .Select(g => new
            {
                Id = g.Key,
                Sent = g.Count(),
                Delivered = g.Count(r => r.DeliveredAt != null),
                Opened = g.Count(r => r.OpenedAt != null),
                Clicked = g.Count(r => r.ClickedAt != null),
                Failed = g.Count(r => r.FailedAt != null),
                Bounced = g.Count(r => r.BouncedAt != null),
            })
            .ToListAsync(ct);

        var result = new Dictionary<string, AutomatedEmailStatistics>(ids.Count);
        foreach (var id in ids)
            result[id] = AutomatedEmailStatistics.Empty;

        foreach (var r in rows)
            result[r.Id] = new AutomatedEmailStatistics(r.Sent, r.Delivered, r.Opened, r.Clicked, r.Failed, r.Bounced);

        return result;
    }

    public async Task<PagedResult<AutomatedEmailRecipient>> GetRecipientsAsync(
        string automatedEmailId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var query = db.AutomatedEmailRecipients
            .Where(r => r.AutomatedEmailId == automatedEmailId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AutomatedEmailRecipient>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<string?> MarkRecipientEventAsync(
        string email, string eventType, DateTime occurredAt, string? failureReason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        // Resolve the most recent recipient row for this address. We only
        // attribute the event to the latest send to reduce mis-attribution
        // across multiple automated emails sent to the same member.
        var recipient = await db.AutomatedEmailRecipients
            .Where(r => r.MemberEmail == email)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recipient is null)
            return null;

        var changed = false;
        switch (eventType)
        {
            case "delivered":
                if (recipient.DeliveredAt is null) { recipient.DeliveredAt = occurredAt; changed = true; }
                break;
            case "opened":
                if (recipient.OpenedAt is null) { recipient.OpenedAt = occurredAt; changed = true; }
                // An open implies delivery
                if (recipient.DeliveredAt is null) { recipient.DeliveredAt = occurredAt; changed = true; }
                break;
            case "clicked":
                if (recipient.ClickedAt is null) { recipient.ClickedAt = occurredAt; changed = true; }
                if (recipient.DeliveredAt is null) { recipient.DeliveredAt = occurredAt; changed = true; }
                break;
            case "failed":
                if (recipient.FailedAt is null) { recipient.FailedAt = occurredAt; changed = true; }
                if (!string.IsNullOrWhiteSpace(failureReason) && string.IsNullOrEmpty(recipient.FailureReason))
                {
                    recipient.FailureReason = failureReason.Length > 2000 ? failureReason[..2000] : failureReason;
                    changed = true;
                }
                break;
            case "bounced":
                if (recipient.BouncedAt is null) { recipient.BouncedAt = occurredAt; changed = true; }
                if (!string.IsNullOrWhiteSpace(failureReason) && string.IsNullOrEmpty(recipient.FailureReason))
                {
                    recipient.FailureReason = failureReason.Length > 2000 ? failureReason[..2000] : failureReason;
                    changed = true;
                }
                break;
            default:
                return null;
        }

        if (changed)
        {
            recipient.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return recipient.Id;
    }

    // ── Drip / sequence (AUTO.4) ──

    public async Task<List<AutomatedEmail>> GetActiveByTriggerAsync(string trigger, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trigger))
            return [];

        return await db.AutomatedEmails
            .Where(e => e.Status == "active" && (e.Slug == trigger || e.TriggerEvent == trigger))
            .ToListAsync(ct);
    }

    public async Task<bool> HasSentOrScheduledAsync(string automatedEmailId, string memberId, CancellationToken ct = default)
    {
        var hasRecipient = await db.AutomatedEmailRecipients
            .AnyAsync(r => r.AutomatedEmailId == automatedEmailId && r.MemberId == memberId, ct);
        if (hasRecipient) return true;

        return await db.AutomatedEmailSchedules
            .AnyAsync(s => s.AutomatedEmailId == automatedEmailId
                        && s.MemberId == memberId
                        && s.ProcessedAt == null, ct);
    }

    public async Task AddScheduleAsync(AutomatedEmailSchedule schedule, CancellationToken ct = default)
    {
        db.AutomatedEmailSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AutomatedEmailSchedule>> GetDueSchedulesAsync(DateTime now, int limit, CancellationToken ct = default)
    {
        if (limit < 1) limit = 50;
        if (limit > 500) limit = 500;

        return await db.AutomatedEmailSchedules
            .Where(s => s.ProcessedAt == null && s.ScheduledFor <= now)
            .OrderBy(s => s.ScheduledFor)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<AutomatedEmailSchedule?> GetScheduleByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.AutomatedEmailSchedules.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task MarkScheduleProcessedAsync(string id, DateTime processedAt, string? failureReason, CancellationToken ct = default)
    {
        var row = await db.AutomatedEmailSchedules.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (row is null) return;
        row.ProcessedAt = processedAt;
        if (!string.IsNullOrWhiteSpace(failureReason))
            row.FailureReason = failureReason.Length > 2000 ? failureReason[..2000] : failureReason;
        await db.SaveChangesAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
