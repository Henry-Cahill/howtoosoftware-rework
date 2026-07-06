using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class EmailRepository(AppDbContext db) : IEmailRepository
{
    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        return new EfTransactionScope(transaction);
    }

    public async Task<Email?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Emails.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<List<Email>> GetPendingEmailsAsync(CancellationToken ct = default)
    {
        return await db.Emails
            .Where(e => e.Status == "pending")
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Email>> GetAbTestsAwaitingWinnerAsync(DateTime now, CancellationToken ct = default)
    {
        // Translate AbTestStartedAt + AbTestWaitMinutes <= now into provider-friendly SQL.
        return await db.Emails
            .Where(e => e.AbTestPhase == "testing" && e.AbTestStartedAt != null)
            .Where(e => EF.Functions.DateDiffMinute(e.AbTestStartedAt!.Value, now) >= e.AbTestWaitMinutes)
            .OrderBy(e => e.AbTestStartedAt)
            .ToListAsync(ct);
    }

    public async Task<List<EmailRecipient>> GetHoldoutRecipientsAsync(string emailId, CancellationToken ct = default)
    {
        return await db.EmailRecipients
            .Where(r => r.EmailId == emailId && r.AbVariant == "holdout" && r.ProcessedAt == null)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<string, int>> GetAbVariantOpenCountsAsync(string emailId, CancellationToken ct = default)
    {
        var counts = await db.EmailRecipients
            .Where(r => r.EmailId == emailId && r.OpenedAt != null && (r.AbVariant == "a" || r.AbVariant == "b"))
            .GroupBy(r => r.AbVariant!)
            .Select(g => new { Variant = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Variant, x => x.Count);
    }

    public async Task AddEmailAsync(Email email, CancellationToken ct = default)
    {
        db.Emails.Add(email);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateEmailAsync(Email email, CancellationToken ct = default)
    {
        db.Emails.Update(email);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddBatchAsync(EmailBatch batch, CancellationToken ct = default)
    {
        db.EmailBatches.Add(batch);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateBatchAsync(EmailBatch batch, CancellationToken ct = default)
    {
        db.EmailBatches.Update(batch);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default)
    {
        db.EmailRecipients.AddRange(recipients);
        await db.SaveChangesAsync(ct);
    }

    public async Task<EmailRecipient?> GetRecipientByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.EmailRecipients.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task UpdateRecipientAsync(EmailRecipient recipient, CancellationToken ct = default)
    {
        db.EmailRecipients.Update(recipient);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default)
    {
        db.EmailRecipients.UpdateRange(recipients);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddRedirectAsync(Redirect redirect, CancellationToken ct = default)
    {
        db.Redirects.Add(redirect);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Redirect?> GetRedirectByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Redirects.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddClickEventAsync(MembersClickEvent clickEvent, CancellationToken ct = default)
    {
        db.MembersClickEvents.Add(clickEvent);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<string>> GetSuppressedEmailsAsync(CancellationToken ct = default)
    {
        return await db.Suppressions
            .Select(s => s.Email)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<bool> IsEmailSuppressedAsync(string email, CancellationToken ct = default)
    {
        return await db.Suppressions.AnyAsync(s => s.Email == email, ct);
    }

    public async Task AddSuppressionAsync(Suppression suppression, CancellationToken ct = default)
    {
        db.Suppressions.Add(suppression);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveSuppressionAsync(string email, CancellationToken ct = default)
    {
        await db.Suppressions.Where(s => s.Email == email).ExecuteDeleteAsync(ct);
    }

    public async Task AddSpamComplaintEventAsync(EmailSpamComplaintEvent evt, CancellationToken ct = default)
    {
        db.EmailSpamComplaintEvents.Add(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MembersNewsletter?> GetSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default)
    {
        return await db.MembersNewsletters
            .FirstOrDefaultAsync(mn => mn.MemberId == memberId && mn.NewsletterId == newsletterId, ct);
    }

    public async Task<List<MembersNewsletter>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default)
    {
        return await db.MembersNewsletters
            .Where(mn => mn.MemberId == memberId)
            .ToListAsync(ct);
    }

    public async Task AddSubscriptionAsync(MembersNewsletter subscription, CancellationToken ct = default)
    {
        db.MembersNewsletters.Add(subscription);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default)
    {
        await db.MembersNewsletters
            .Where(mn => mn.MemberId == memberId && mn.NewsletterId == newsletterId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<MembersFeedback?> GetFeedbackAsync(string memberId, string postId, CancellationToken ct = default)
    {
        return await db.MembersFeedback
            .FirstOrDefaultAsync(f => f.MemberId == memberId && f.PostId == postId, ct);
    }

    public async Task AddFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default)
    {
        db.MembersFeedback.Add(feedback);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default)
    {
        db.MembersFeedback.Update(feedback);
        await db.SaveChangesAsync(ct);
    }

    private sealed class EfTransactionScope(IDbContextTransaction transaction) : ITransactionScope
    {
        public async Task CommitAsync(CancellationToken ct = default)
        {
            await transaction.CommitAsync(ct);
        }

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
