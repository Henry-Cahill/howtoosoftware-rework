using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Services;

public class MemberActivityService(AppDbContext db) : IMemberActivityService
{
    public async Task<IReadOnlyList<MemberActivityItem>> GetForMemberAsync(string memberId, int top = 100, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(memberId)) return [];
        if (top <= 0) top = 100;

        var items = new List<MemberActivityItem>(capacity: 128);

        // Member created (signup)
        var created = await db.MembersCreatedEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .Select(e => new { e.CreatedAt, e.Source, e.AttributionUrl, e.ReferrerSource })
            .ToListAsync(ct);
        foreach (var e in created)
        {
            var desc = e.ReferrerSource is not null
                ? $"Source: {e.Source} · Referrer: {e.ReferrerSource}"
                : $"Source: {e.Source}";
            items.Add(new MemberActivityItem(e.CreatedAt, "signup", "Signed up", desc));
        }

        // Logins
        var logins = await db.MembersLoginEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(top)
            .Select(e => e.CreatedAt)
            .ToListAsync(ct);
        foreach (var ts in logins)
        {
            items.Add(new MemberActivityItem(ts, "login", "Signed in"));
        }

        // Status changes (e.g. free -> paid)
        var statuses = await db.MembersStatusEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .Select(e => new { e.CreatedAt, e.FromStatus, e.ToStatus })
            .ToListAsync(ct);
        foreach (var e in statuses)
        {
            var title = e.FromStatus is null
                ? $"Status set to {e.ToStatus}"
                : $"Status changed: {e.FromStatus} → {e.ToStatus}";
            items.Add(new MemberActivityItem(e.CreatedAt, "status_change", title));
        }

        // Newsletter subscribe / unsubscribe
        var subscribes = await db.MembersSubscribeEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .Select(e => new
            {
                e.CreatedAt,
                e.Subscribed,
                e.Source,
                NewsletterName = e.Newsletter != null ? e.Newsletter.Name : null,
            })
            .ToListAsync(ct);
        foreach (var e in subscribes)
        {
            var title = e.Subscribed ? "Subscribed to newsletter" : "Unsubscribed from newsletter";
            var desc = e.NewsletterName is not null
                ? (e.Source is not null ? $"{e.NewsletterName} · {e.Source}" : e.NewsletterName)
                : e.Source;
            items.Add(new MemberActivityItem(
                e.CreatedAt,
                e.Subscribed ? "subscribed" : "unsubscribed",
                title,
                desc));
        }

        // Email deliveries — sent / opened (recent only, by EmailId join to Email for subject + post)
        // Newest first by ProcessedAt to bound the volume.
        var recipients = await db.EmailRecipients.AsNoTracking()
            .Where(r => r.MemberId == memberId)
            .OrderByDescending(r => r.ProcessedAt)
            .Take(top)
            .Select(r => new
            {
                r.ProcessedAt,
                r.OpenedAt,
                r.EmailId,
                EmailSubject = db.Emails.Where(em => em.Id == r.EmailId).Select(em => em.Subject).FirstOrDefault(),
                PostSlug = db.Emails.Where(em => em.Id == r.EmailId)
                    .Join(db.Posts, em => em.PostId, p => p.Id, (em, p) => p.Slug).FirstOrDefault(),
                PostId = db.Emails.Where(em => em.Id == r.EmailId).Select(em => em.PostId).FirstOrDefault(),
            })
            .ToListAsync(ct);
        foreach (var r in recipients)
        {
            var subject = r.EmailSubject ?? "Newsletter";
            var url = r.PostId is not null ? $"/posts/{r.PostId}/edit" : null;
            if (r.ProcessedAt is { } sent)
            {
                items.Add(new MemberActivityItem(sent, "email_sent", "Email sent", subject, subject, url));
            }
            if (r.OpenedAt is { } opened)
            {
                items.Add(new MemberActivityItem(opened, "email_opened", "Email opened", subject, subject, url));
            }
        }

        // Email link clicks
        var clicks = await db.MembersClickEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(top)
            .Select(e => new
            {
                e.CreatedAt,
                RedirectTo = e.Redirect != null ? e.Redirect.To : null,
            })
            .ToListAsync(ct);
        foreach (var c in clicks)
        {
            items.Add(new MemberActivityItem(
                c.CreatedAt,
                "email_clicked",
                "Clicked email link",
                c.RedirectTo,
                c.RedirectTo,
                c.RedirectTo));
        }

        // Comments
        var comments = await db.Comments.AsNoTracking()
            .Where(c => c.MemberId == memberId && c.Status == "published")
            .OrderByDescending(c => c.CreatedAt)
            .Take(top)
            .Select(c => new
            {
                c.CreatedAt,
                c.PostId,
                PostTitle = c.Post.Title,
            })
            .ToListAsync(ct);
        foreach (var c in comments)
        {
            items.Add(new MemberActivityItem(
                c.CreatedAt,
                "comment",
                "Posted a comment",
                c.PostTitle,
                c.PostTitle,
                $"/posts/{c.PostId}/edit"));
        }

        // Post feedback (more/less like this)
        var feedbacks = await db.MembersFeedback.AsNoTracking()
            .Where(f => f.MemberId == memberId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(top)
            .Select(f => new
            {
                f.CreatedAt,
                f.Score,
                f.PostId,
                PostTitle = f.Post.Title,
            })
            .ToListAsync(ct);
        foreach (var f in feedbacks)
        {
            var verdict = f.Score > 0 ? "More like this" : "Less like this";
            items.Add(new MemberActivityItem(
                f.CreatedAt,
                "feedback",
                $"Gave feedback: {verdict}",
                f.PostTitle,
                f.PostTitle,
                $"/posts/{f.PostId}/edit"));
        }

        // Subscription created
        var subCreated = await db.MembersSubscriptionCreatedEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .Select(e => e.CreatedAt)
            .ToListAsync(ct);
        foreach (var ts in subCreated)
        {
            items.Add(new MemberActivityItem(ts, "subscription_created", "Subscription created"));
        }

        // Paid subscription changes (upgrades / downgrades)
        var paidEvents = await db.MembersPaidSubscriptionEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .Select(e => new { e.CreatedAt, e.Type, e.FromPlan, e.ToPlan, e.MrrDelta, e.Currency })
            .ToListAsync(ct);
        foreach (var e in paidEvents)
        {
            string title;
            if (string.Equals(e.Type, "created", StringComparison.OrdinalIgnoreCase))
                title = "Paid subscription started";
            else if (string.Equals(e.Type, "canceled", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(e.Type, "cancelled", StringComparison.OrdinalIgnoreCase))
                title = "Paid subscription cancelled";
            else if (e.FromPlan is not null && e.ToPlan is not null && e.FromPlan != e.ToPlan)
                title = $"Plan changed: {e.FromPlan} → {e.ToPlan}";
            else
                title = "Subscription updated";

            var mrr = e.MrrDelta != 0
                ? $"MRR Δ {(e.MrrDelta > 0 ? "+" : "")}{(e.MrrDelta / 100m).ToString("F2")} {e.Currency?.ToUpperInvariant()}"
                : null;
            items.Add(new MemberActivityItem(e.CreatedAt, "subscription_changed", title, mrr));
        }

        // Cancellations (explicit)
        var cancels = await db.MembersCancelEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .Select(e => new { e.CreatedAt, e.FromPlan })
            .ToListAsync(ct);
        foreach (var e in cancels)
        {
            items.Add(new MemberActivityItem(e.CreatedAt, "subscription_cancelled", "Subscription cancelled", e.FromPlan));
        }

        // Payments
        var payments = await db.MembersPaymentEvents.AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(top)
            .Select(e => new { e.CreatedAt, e.Amount, e.Currency, e.Source })
            .ToListAsync(ct);
        foreach (var p in payments)
        {
            var amount = $"{(p.Amount / 100m).ToString("F2")} {p.Currency?.ToUpperInvariant()}";
            items.Add(new MemberActivityItem(p.CreatedAt, "payment", $"Payment received: {amount}", p.Source));
        }

        // Sort newest first, cap to requested size.
        return items
            .OrderByDescending(i => i.Timestamp)
            .Take(top)
            .ToList();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
