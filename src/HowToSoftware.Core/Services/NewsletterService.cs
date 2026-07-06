using System.Security.Cryptography;
using System.Text.RegularExpressions;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Core.Services;

public partial class NewsletterService(
    IPostRepository postRepository,
    INewsletterRepository newsletterRepository,
    IMemberRepository memberRepository,
    IEmailRepository emailRepository,
    IEmailService emailService,
    ISettingsService settingsService) : INewsletterService
{
    public async Task<Email> SendPostAsNewsletterAsync(SendNewsletterRequest request, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(request.PostId, ct)
            ?? throw new InvalidOperationException($"Post '{request.PostId}' not found.");

        if (string.IsNullOrWhiteSpace(post.Html))
            throw new InvalidOperationException("Cannot send a post with no rendered content as email.");

        var newsletter = await newsletterRepository.GetByIdAsync(request.NewsletterId, ct)
            ?? throw new InvalidOperationException($"Newsletter '{request.NewsletterId}' not found.");

        if (newsletter.Status != "active")
            throw new InvalidOperationException("Cannot send email for an inactive newsletter.");

        // Get subscribed members, excluding suppressed emails
        var subscribers = await memberRepository.GetNewsletterSubscribersAsync(request.NewsletterId, ct);
        var suppressedEmails = await emailRepository.GetSuppressedEmailsAsync(ct);
        var suppressedSet = new HashSet<string>(suppressedEmails, StringComparer.OrdinalIgnoreCase);

        subscribers = subscribers
            .Where(m => !suppressedSet.Contains(m.Email))
            .ToList();

        // Apply recipient filter
        if (request.RecipientFilter != "all")
        {
            subscribers = subscribers
                .Where(m => m.Status == request.RecipientFilter)
                .ToList();
        }

        if (subscribers.Count == 0)
            throw new InvalidOperationException("No eligible recipients for this newsletter.");

        var now = DateTime.UtcNow;
        var siteUrl = request.SiteUrl.TrimEnd('/');

        // Build sender info
        var senderName = newsletter.SenderName
            ?? await settingsService.GetStringAsync("title", ct)
            ?? "Newsletter";
        var senderEmail = newsletter.SenderEmail
            ?? await settingsService.GetStringAsync("members_support_address", ct)
            ?? "noreply@example.com";
        var fromAddress = $"{senderName} <{senderEmail}>";
        var replyTo = ResolveReplyTo(newsletter.SenderReplyTo, senderEmail);

        // Resolve A/B subject: explicit request override wins, else PostMeta.EmailSubjectB.
        var subjectA = post.Meta?.EmailSubject ?? post.Title;
        var subjectB = !string.IsNullOrWhiteSpace(request.SubjectB)
            ? request.SubjectB
            : post.Meta?.EmailSubjectB;
        var abEnabled = !string.IsNullOrWhiteSpace(subjectB);
        var splitPercent = Math.Clamp(request.AbSplitPercent, 1, 50);
        var waitMinutes = Math.Max(0, request.AbWaitMinutes);

        // Need at least one recipient per variant cohort and one holdout to make A/B meaningful.
        // With splitPercent% per cohort, we need >= (2 * perCohort + 1) subscribers; require min 5.
        if (abEnabled && subscribers.Count < 5)
        {
            abEnabled = false;
            subjectB = null;
        }

        List<Member> cohortA;
        List<Member> cohortB;
        List<Member> holdout;
        if (abEnabled)
        {
            (cohortA, cohortB, holdout) = PartitionForAbTest(subscribers, splitPercent);
        }
        else
        {
            cohortA = subscribers;
            cohortB = new List<Member>();
            holdout = new List<Member>();
        }

        // ── Phase 1: Atomically persist redirects, email, batch, and recipients ──
        await using var setupTxn = await emailRepository.BeginTransactionAsync(ct);

        // Wrap links for click tracking → creates Redirect records
        var (trackedHtml, redirects) = await WrapLinksForClickTrackingAsync(
            post.Html, post.Id, siteUrl, ct);

        // Create Email record
        var email = new Email
        {
            Id = GenerateId(),
            PostId = post.Id,
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "submitting",
            RecipientFilter = request.RecipientFilter,
            Subject = subjectA,
            SubjectB = abEnabled ? subjectB : null,
            From = fromAddress,
            ReplyTo = replyTo,
            Html = trackedHtml,
            Plaintext = post.Plaintext,
            Source = post.Lexical ?? post.Mobiledoc,
            SourceType = post.Lexical is not null ? "lexical" : "mobiledoc",
            TrackOpens = true,
            TrackClicks = redirects.Count > 0,
            FeedbackEnabled = newsletter.FeedbackEnabled,
            EmailCount = subscribers.Count,
            NewsletterId = newsletter.Id,
            SubmittedAt = now,
            CreatedAt = now,
            AbTestPhase = abEnabled ? "testing" : null,
            AbTestSplitPercent = abEnabled ? splitPercent : 0,
            AbTestWaitMinutes = abEnabled ? waitMinutes : 0,
        };

        await emailRepository.AddEmailAsync(email, ct);

        // Build one batch per active cohort. In A/B mode cohortB is non-empty.
        var batchA = new EmailBatch
        {
            Id = GenerateId(),
            EmailId = email.Id,
            Status = "submitting",
            CreatedAt = now,
            UpdatedAt = now,
        };
        await emailRepository.AddBatchAsync(batchA, ct);

        EmailBatch? batchB = null;
        if (abEnabled && cohortB.Count > 0)
        {
            batchB = new EmailBatch
            {
                Id = GenerateId(),
                EmailId = email.Id,
                Status = "submitting",
                CreatedAt = now,
                UpdatedAt = now,
            };
            await emailRepository.AddBatchAsync(batchB, ct);
        }

        // Create recipient records — cohort A and B are processed now; holdout is
        // persisted with ProcessedAt=null and picked up later by EmailAbTestWinnerService.
        var recipientsA = cohortA.Select(member => new EmailRecipient
        {
            Id = GenerateId(),
            EmailId = email.Id,
            BatchId = batchA.Id,
            MemberId = member.Id,
            MemberUuid = member.Uuid,
            MemberEmail = member.Email,
            MemberName = member.Name,
            ProcessedAt = now,
            AbVariant = abEnabled ? "a" : null,
        }).ToList();

        var recipientsB = cohortB.Select(member => new EmailRecipient
        {
            Id = GenerateId(),
            EmailId = email.Id,
            BatchId = batchB!.Id,
            MemberId = member.Id,
            MemberUuid = member.Uuid,
            MemberEmail = member.Email,
            MemberName = member.Name,
            ProcessedAt = now,
            AbVariant = "b",
        }).ToList();

        var holdoutRecipients = holdout.Select(member => new EmailRecipient
        {
            Id = GenerateId(),
            EmailId = email.Id,
            BatchId = batchA.Id, // placeholder; reassigned at winner send
            MemberId = member.Id,
            MemberUuid = member.Uuid,
            MemberEmail = member.Email,
            MemberName = member.Name,
            ProcessedAt = null,
            AbVariant = "holdout",
        }).ToList();

        if (recipientsA.Count > 0) await emailRepository.AddRecipientsAsync(recipientsA, ct);
        if (recipientsB.Count > 0) await emailRepository.AddRecipientsAsync(recipientsB, ct);
        if (holdoutRecipients.Count > 0) await emailRepository.AddRecipientsAsync(holdoutRecipients, ct);

        await setupTxn.CommitAsync(ct);

        // Render & send via email service — each recipient gets personalized HTML
        // with their own open-tracking pixel
        var siteTitle = await settingsService.GetStringAsync("title", ct);
        var siteIconUrl = await settingsService.GetStringAsync("icon", ct);
        var accentColor = await settingsService.GetStringAsync("accent_color", ct);

        // Resolve primary author name if available
        string? authorName = post.PostsAuthors?.OrderBy(pa => pa.SortOrder).FirstOrDefault()?.Author?.Name;

        var templateModel = BuildTemplateModel(post, newsletter, siteUrl, siteTitle, siteIconUrl, accentColor, authorName);
        var renderedTemplate = await emailService.RenderTemplateAsync("newsletter", templateModel, ct);

        // Send each active cohort with its own subject line.
        var (deliveredA, failedA, updatedA) = await SendCohortAsync(
            recipientsA, subjectA, renderedTemplate, fromAddress, replyTo,
            email, siteUrl, newsletter.Id, ct);

        var deliveredB = 0;
        var failedB = 0;
        var updatedB = new List<EmailRecipient>();
        if (recipientsB.Count > 0 && !string.IsNullOrWhiteSpace(subjectB))
        {
            (deliveredB, failedB, updatedB) = await SendCohortAsync(
                recipientsB, subjectB!, renderedTemplate, fromAddress, replyTo,
                email, siteUrl, newsletter.Id, ct);
        }

        // ── Phase 3: Atomically update delivery statuses ──
        await using var statusTxn = await emailRepository.BeginTransactionAsync(ct);

        if (updatedA.Count > 0) await emailRepository.UpdateRecipientsAsync(updatedA, ct);
        if (updatedB.Count > 0) await emailRepository.UpdateRecipientsAsync(updatedB, ct);

        batchA.Status = failedA == recipientsA.Count && recipientsA.Count > 0 ? "failed" : "submitted";
        batchA.UpdatedAt = DateTime.UtcNow;
        await emailRepository.UpdateBatchAsync(batchA, ct);

        if (batchB is not null)
        {
            batchB.Status = failedB == recipientsB.Count && recipientsB.Count > 0 ? "failed" : "submitted";
            batchB.UpdatedAt = DateTime.UtcNow;
            await emailRepository.UpdateBatchAsync(batchB, ct);
        }

        var deliveredCount = deliveredA + deliveredB;
        var failedCount = failedA + failedB;
        var sentCount = recipientsA.Count + recipientsB.Count;

        // In A/B mode the email is not yet "submitted" overall — it will reach final
        // status only after the holdout send. We still record progress so far.
        email.Status = sentCount > 0 && failedCount == sentCount
            ? "failed"
            : abEnabled
                ? "submitting" // remains "submitting" until winner-send completes
                : "submitted";
        if (abEnabled)
        {
            email.AbTestStartedAt = DateTime.UtcNow;
        }
        email.DeliveredCount = deliveredCount;
        email.FailedCount = failedCount;
        email.UpdatedAt = DateTime.UtcNow;
        await emailRepository.UpdateEmailAsync(email, ct);

        await statusTxn.CommitAsync(ct);

        return email;
    }

    public async Task ProcessPendingEmailAsync(string emailId, string siteUrl, CancellationToken ct = default)
    {
        var email = await emailRepository.GetByIdAsync(emailId, ct)
            ?? throw new InvalidOperationException($"Email '{emailId}' not found.");

        if (email.Status != "pending")
            return; // Already picked up or processed

        var post = await postRepository.GetByIdAsync(email.PostId, ct)
            ?? throw new InvalidOperationException($"Post '{email.PostId}' not found for email '{emailId}'.");

        var newsletterId = email.NewsletterId
            ?? throw new InvalidOperationException($"Email '{emailId}' has no newsletter assigned.");

        var newsletter = await newsletterRepository.GetByIdAsync(newsletterId, ct)
            ?? throw new InvalidOperationException($"Newsletter '{newsletterId}' not found for email '{emailId}'.");

        if (newsletter.Status != "active")
            throw new InvalidOperationException($"Newsletter '{newsletterId}' is not active.");

        siteUrl = siteUrl.TrimEnd('/');
        var now = DateTime.UtcNow;

        // Mark as submitting immediately to prevent re-pickup
        email.Status = "submitting";
        email.UpdatedAt = now;
        await emailRepository.UpdateEmailAsync(email, ct);

        // Get subscribed members, excluding suppressed emails
        var subscribers = await memberRepository.GetNewsletterSubscribersAsync(newsletterId, ct);
        var suppressedEmails = await emailRepository.GetSuppressedEmailsAsync(ct);
        var suppressedSet = new HashSet<string>(suppressedEmails, StringComparer.OrdinalIgnoreCase);

        subscribers = subscribers
            .Where(m => !suppressedSet.Contains(m.Email))
            .ToList();

        // Apply recipient filter
        if (email.RecipientFilter != "all")
        {
            subscribers = subscribers
                .Where(m => m.Status == email.RecipientFilter)
                .ToList();
        }

        if (subscribers.Count == 0)
        {
            email.Status = "submitted";
            email.EmailCount = 0;
            email.UpdatedAt = DateTime.UtcNow;
            await emailRepository.UpdateEmailAsync(email, ct);
            return;
        }

        // Build sender info
        var senderName = newsletter.SenderName
            ?? await settingsService.GetStringAsync("title", ct)
            ?? "Newsletter";
        var senderEmail = newsletter.SenderEmail
            ?? await settingsService.GetStringAsync("members_support_address", ct)
            ?? "noreply@example.com";
        var fromAddress = $"{senderName} <{senderEmail}>";
        var replyTo = ResolveReplyTo(newsletter.SenderReplyTo, senderEmail);

        // ── A/B test resolution: triggered when Email.SubjectB is non-null and we
        // have enough subscribers to make a cohort comparison meaningful.
        var abEnabled = !string.IsNullOrWhiteSpace(email.SubjectB) && subscribers.Count >= 5;
        var splitPercent = abEnabled
            ? Math.Clamp(email.AbTestSplitPercent > 0 ? email.AbTestSplitPercent : 10, 1, 50)
            : 0;
        var waitMinutes = abEnabled
            ? Math.Max(0, email.AbTestWaitMinutes > 0 ? email.AbTestWaitMinutes : 120)
            : 0;

        List<Member> cohortA;
        List<Member> cohortB;
        List<Member> holdout;
        if (abEnabled)
        {
            (cohortA, cohortB, holdout) = PartitionForAbTest(subscribers, splitPercent);
        }
        else
        {
            cohortA = subscribers;
            cohortB = new List<Member>();
            holdout = new List<Member>();
        }

        // ── Atomically persist redirects and update email metadata ──
        await using (var setupTxn = await emailRepository.BeginTransactionAsync(ct))
        {
            // Wrap links for click tracking
            var (trackedHtml, _) = await WrapLinksForClickTrackingAsync(
                post.Html!, post.Id, siteUrl, ct);

            email.From = fromAddress;
            email.ReplyTo = replyTo;
            email.Html = trackedHtml;
            email.EmailCount = subscribers.Count;
            if (abEnabled)
            {
                email.AbTestPhase = "testing";
                email.AbTestSplitPercent = splitPercent;
                email.AbTestWaitMinutes = waitMinutes;
            }
            else
            {
                // Ensure stale A/B fields don't influence later code paths.
                email.SubjectB = null;
                email.AbTestPhase = null;
                email.AbTestSplitPercent = 0;
                email.AbTestWaitMinutes = 0;
            }

            await emailRepository.UpdateEmailAsync(email, ct);
            await setupTxn.CommitAsync(ct);
        }

        // Render template
        var siteTitle = await settingsService.GetStringAsync("title", ct);
        var siteIconUrl = await settingsService.GetStringAsync("icon", ct);
        var accentColor = await settingsService.GetStringAsync("accent_color", ct);
        string? authorName = post.PostsAuthors?.OrderBy(pa => pa.SortOrder).FirstOrDefault()?.Author?.Name;

        var templateModel = BuildTemplateModel(post, newsletter, siteUrl, siteTitle, siteIconUrl, accentColor, authorName);
        var renderedTemplate = await emailService.RenderTemplateAsync("newsletter", templateModel, ct);

        const int batchSize = 500;
        var totalDelivered = 0;
        var totalFailed = 0;

        async Task SendCohortChunkedAsync(List<Member> cohort, string subject, string? abVariant)
        {
            for (var offset = 0; offset < cohort.Count; offset += batchSize)
            {
                var chunk = cohort.Skip(offset).Take(batchSize).ToList();

                // ── Per-batch Phase 1: Atomically persist batch and recipients ──
                EmailBatch batch;
                List<EmailRecipient> recipients;

                await using (var batchSetupTxn = await emailRepository.BeginTransactionAsync(ct))
                {
                    batch = new EmailBatch
                    {
                        Id = GenerateId(),
                        EmailId = email.Id,
                        Status = "submitting",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    await emailRepository.AddBatchAsync(batch, ct);

                    recipients = chunk.Select(member => new EmailRecipient
                    {
                        Id = GenerateId(),
                        EmailId = email.Id,
                        BatchId = batch.Id,
                        MemberId = member.Id,
                        MemberUuid = member.Uuid,
                        MemberEmail = member.Email,
                        MemberName = member.Name,
                        ProcessedAt = DateTime.UtcNow,
                        AbVariant = abVariant,
                    }).ToList();

                    await emailRepository.AddRecipientsAsync(recipients, ct);
                    await batchSetupTxn.CommitAsync(ct);
                }

                // ── Per-batch Phase 2: External MTA call (outside transaction) ──
                var batchRequest = new EmailBatchRequest
                {
                    From = fromAddress,
                    ReplyTo = replyTo,
                    Subject = subject,
                    Html = renderedTemplate,
                    Plaintext = email.Plaintext ?? string.Empty,
                    Recipients = BuildRecipientSubstitutions(recipients, email.Id, siteUrl, newsletterId),
                };

                var results = await emailService.SendBatchAsync(batchRequest, ct);

                // ── Per-batch Phase 3: Atomically update delivery statuses ──
                var batchDelivered = 0;
                var batchFailed = 0;
                var updatedRecipients = new List<EmailRecipient>();

                await using (var batchStatusTxn = await emailRepository.BeginTransactionAsync(ct))
                {
                    foreach (var result in results)
                    {
                        var recipient = recipients.FirstOrDefault(r => r.MemberEmail == result.RecipientEmail);
                        if (recipient is null) continue;

                        if (result.Success)
                        {
                            recipient.DeliveredAt = DateTime.UtcNow;
                            batchDelivered++;
                        }
                        else
                        {
                            recipient.FailedAt = DateTime.UtcNow;
                            batchFailed++;

                            recipient.Failures.Add(new EmailRecipientFailure
                            {
                                Id = GenerateId(),
                                EmailId = email.Id,
                                MemberId = recipient.MemberId,
                                EmailRecipientId = recipient.Id,
                                Code = result.ErrorCode ?? 0,
                                Message = result.ErrorMessage ?? "Unknown error",
                                Severity = "permanent",
                                FailedAt = DateTime.UtcNow,
                            });
                        }

                        updatedRecipients.Add(recipient);
                    }

                    await emailRepository.UpdateRecipientsAsync(updatedRecipients, ct);

                    batch.Status = batchFailed == chunk.Count ? "failed" : "submitted";
                    batch.UpdatedAt = DateTime.UtcNow;
                    await emailRepository.UpdateBatchAsync(batch, ct);

                    await batchStatusTxn.CommitAsync(ct);
                }

                totalDelivered += batchDelivered;
                totalFailed += batchFailed;
            }
        }

        // Send variant A (or the entire audience when A/B is disabled)
        await SendCohortChunkedAsync(cohortA, email.Subject!, abEnabled ? "a" : null);

        // Send variant B
        if (abEnabled && cohortB.Count > 0)
        {
            await SendCohortChunkedAsync(cohortB, email.SubjectB!, "b");
        }

        // Persist holdout recipients (no send). They are picked up later by
        // EmailAbTestWinnerService once the wait window elapses.
        if (abEnabled && holdout.Count > 0)
        {
            await using var holdoutTxn = await emailRepository.BeginTransactionAsync(ct);
            // Holdout recipients share batchA conceptually; assign a placeholder batch row
            // so the NOT NULL FK is satisfied. The winner-send reassigns BatchId per chunk.
            var placeholderBatch = new EmailBatch
            {
                Id = GenerateId(),
                EmailId = email.Id,
                Status = "submitting",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            await emailRepository.AddBatchAsync(placeholderBatch, ct);

            var holdoutRecipients = holdout.Select(member => new EmailRecipient
            {
                Id = GenerateId(),
                EmailId = email.Id,
                BatchId = placeholderBatch.Id,
                MemberId = member.Id,
                MemberUuid = member.Uuid,
                MemberEmail = member.Email,
                MemberName = member.Name,
                ProcessedAt = null,
                AbVariant = "holdout",
            }).ToList();

            await emailRepository.AddRecipientsAsync(holdoutRecipients, ct);
            await holdoutTxn.CommitAsync(ct);
        }

        // Finalize email status. In A/B mode we remain in "submitting" until the
        // winner-send completes; otherwise we report "submitted"/"failed" now.
        var sentCount = cohortA.Count + cohortB.Count;
        email.Status = sentCount > 0 && totalFailed == sentCount
            ? "failed"
            : abEnabled
                ? "submitting"
                : "submitted";
        if (abEnabled)
        {
            email.AbTestStartedAt = DateTime.UtcNow;
        }
        email.DeliveredCount = totalDelivered;
        email.FailedCount = totalFailed;
        email.UpdatedAt = DateTime.UtcNow;
        await emailRepository.UpdateEmailAsync(email, ct);
    }

    public async Task RecordOpenAsync(string emailRecipientId, CancellationToken ct = default)
    {
        var recipient = await emailRepository.GetRecipientByIdAsync(emailRecipientId, ct);
        if (recipient is null) return;

        // Only record the first open time
        if (recipient.OpenedAt is not null) return;

        recipient.OpenedAt = DateTime.UtcNow;
        await emailRepository.UpdateRecipientAsync(recipient, ct);

        // Increment the email-level opened count
        var email = await emailRepository.GetByIdAsync(recipient.EmailId, ct);
        if (email is not null)
        {
            email.OpenedCount++;
            email.UpdatedAt = DateTime.UtcNow;
            await emailRepository.UpdateEmailAsync(email, ct);
        }
    }

    public async Task<string> RecordClickAsync(string redirectId, string memberId, CancellationToken ct = default)
    {
        var redirect = await emailRepository.GetRedirectByIdAsync(redirectId, ct)
            ?? throw new InvalidOperationException($"Redirect '{redirectId}' not found.");

        await emailRepository.AddClickEventAsync(new MembersClickEvent
        {
            Id = GenerateId(),
            MemberId = memberId,
            RedirectId = redirectId,
            CreatedAt = DateTime.UtcNow,
        }, ct);

        return redirect.To;
    }

    public async Task SubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default)
    {
        var existing = await emailRepository.GetSubscriptionAsync(memberId, newsletterId, ct);
        if (existing is not null) return;

        await emailRepository.AddSubscriptionAsync(new MembersNewsletter
        {
            Id = GenerateId(),
            MemberId = memberId,
            NewsletterId = newsletterId,
        }, ct);
    }

    public async Task UnsubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default)
    {
        await emailRepository.RemoveSubscriptionAsync(memberId, newsletterId, ct);
    }

    public async Task<List<string>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default)
    {
        var subs = await emailRepository.GetMemberSubscriptionsAsync(memberId, ct);
        return subs.Select(s => s.NewsletterId).ToList();
    }

    public async Task RecordFeedbackAsync(string emailId, string memberId, int score, CancellationToken ct = default)
    {
        var email = await emailRepository.GetByIdAsync(emailId, ct)
            ?? throw new InvalidOperationException($"Email '{emailId}' not found.");

        var postId = email.PostId;

        var existing = await emailRepository.GetFeedbackAsync(memberId, postId, ct);
        if (existing is not null)
        {
            existing.Score = score;
            existing.UpdatedAt = DateTime.UtcNow;
            await emailRepository.UpdateFeedbackAsync(existing, ct);
        }
        else
        {
            await emailRepository.AddFeedbackAsync(new MembersFeedback
            {
                Id = GenerateId(),
                MemberId = memberId,
                PostId = postId,
                Score = score,
                CreatedAt = DateTime.UtcNow,
            }, ct);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Wraps all &lt;a href="..."&gt; links in the HTML with click-tracking redirect URLs.
    /// Returns the modified HTML and the list of Redirect entities created.
    /// </summary>
    private async Task<(string html, List<Redirect> redirects)> WrapLinksForClickTrackingAsync(
        string html, string postId, string siteUrl, CancellationToken ct)
    {
        var redirects = new List<Redirect>();
        // Track which destination URLs we've already created redirects for
        var urlToRedirect = new Dictionary<string, Redirect>(StringComparer.OrdinalIgnoreCase);

        var result = HrefRegex().Replace(html, match =>
        {
            var originalUrl = match.Groups[1].Value;

            // Skip anchors, mailto, tel, and tracking pixel URLs
            if (originalUrl.StartsWith('#')
                || originalUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || originalUrl.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                || originalUrl.Contains("/api/email/open/", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            if (!urlToRedirect.TryGetValue(originalUrl, out var redirect))
            {
                redirect = new Redirect
                {
                    Id = GenerateId(),
                    From = $"/r/{GenerateId()}",
                    To = originalUrl,
                    PostId = postId,
                    CreatedAt = DateTime.UtcNow,
                };
                urlToRedirect[originalUrl] = redirect;
                redirects.Add(redirect);
            }

            var trackingUrl = $"{siteUrl}/api/email/click/{redirect.Id}/%%member_id%%";
            return $"href=\"{trackingUrl}\"";
        });

        // Persist redirect records
        foreach (var redirect in redirects)
        {
            await emailRepository.AddRedirectAsync(redirect, ct);
        }

        return (result, redirects);
    }

    /// <summary>
    /// Builds per-recipient substitution dictionaries containing
    /// the tracking pixel, unsubscribe link, and member-specific tokens.
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> BuildRecipientSubstitutions(
        List<EmailRecipient> recipients, string emailId, string siteUrl, string newsletterId)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        foreach (var recipient in recipients)
        {
            // Open-tracking pixel: 1x1 transparent GIF served by the app
            var openPixelUrl = $"{siteUrl}/api/email/open/{recipient.Id}";
            var unsubscribeUrl = $"{siteUrl}/unsubscribe/?uuid={recipient.MemberUuid}&newsletter={newsletterId}";

            result[recipient.MemberEmail] = new Dictionary<string, string>
            {
                ["tracking_pixel"] = $"<img src=\"{openPixelUrl}\" width=\"1\" height=\"1\" border=\"0\" style=\"height:1px!important;width:1px!important;border-width:0!important;margin:0!important;padding:0!important\" alt=\"\" />",
                ["unsubscribe_url"] = unsubscribeUrl,
                ["member_id"] = recipient.MemberId,
                ["member_uuid"] = recipient.MemberUuid,
                ["member_email"] = recipient.MemberEmail,
                ["member_name"] = recipient.MemberName ?? string.Empty,
                ["feedback_positive_url"] = $"{siteUrl}/api/email/feedback/{emailId}/{recipient.MemberId}/1",
                ["feedback_negative_url"] = $"{siteUrl}/api/email/feedback/{emailId}/{recipient.MemberId}/0",
            };
        }

        return result;
    }

    private static EmailTemplateModel BuildTemplateModel(
        Post post, Newsletter newsletter, string siteUrl,
        string? siteTitle, string? siteIconUrl, string? accentColor, string? authorName)
    {
        return new EmailTemplateModel
        {
            // Site settings
            SiteTitle = siteTitle,
            SiteUrl = siteUrl,
            SiteIconUrl = siteIconUrl,
            AccentColor = accentColor,

            // Post content
            PostTitle = post.Title,
            PostUrl = $"{siteUrl}/{post.Slug}/",
            FeatureImage = post.FeatureImage,
            HtmlBody = post.Html,
            AuthorName = authorName,
            Excerpt = post.CustomExcerpt,
            PublishedAt = post.PublishedAt,

            // Newsletter design — visibility toggles
            HeaderImage = newsletter.HeaderImage,
            ShowBadge = newsletter.ShowBadge,
            ShowHeaderIcon = newsletter.ShowHeaderIcon,
            ShowHeaderTitle = newsletter.ShowHeaderTitle,
            ShowHeaderName = newsletter.ShowHeaderName,
            ShowFeatureImage = newsletter.ShowFeatureImage,
            ShowExcerpt = newsletter.ShowExcerpt,
            ShowPostTitleSection = newsletter.ShowPostTitleSection,
            ShowCommentCta = newsletter.ShowCommentCta,
            FeedbackEnabled = newsletter.FeedbackEnabled,

            // Newsletter design — typography & colors
            BackgroundColor = newsletter.BackgroundColor,
            TitleFontCategory = newsletter.TitleFontCategory,
            TitleAlignment = newsletter.TitleAlignment,
            TitleFontWeight = newsletter.TitleFontWeight,
            PostTitleColor = newsletter.PostTitleColor,
            BodyFontCategory = newsletter.BodyFontCategory,
            HeaderBackgroundColor = newsletter.HeaderBackgroundColor,
            DividerColor = newsletter.DividerColor,

            // Newsletter design — buttons & links
            ButtonCorners = newsletter.ButtonCorners,
            ButtonStyle = newsletter.ButtonStyle,
            ButtonColor = newsletter.ButtonColor,
            LinkStyle = newsletter.LinkStyle,
            LinkColor = newsletter.LinkColor,
            ImageCorners = newsletter.ImageCorners,

            // Footer
            FooterContent = newsletter.FooterContent,
            UnsubscribeUrl = "%%unsubscribe_url%%",
            ManageSubscriptionUrl = $"{siteUrl}/#/portal/account",
            CommentUrl = $"{siteUrl}/{post.Slug}/#comments",
        };
    }

    private static string? ResolveReplyTo(string senderReplyTo, string senderEmail)
    {
        return senderReplyTo switch
        {
            "newsletter" => senderEmail,
            "support" => null, // Falls back to general support address
            _ => senderReplyTo, // Custom address
        };
    }

    /// <summary>
    /// Sends a single cohort of recipients via the email service and returns the
    /// delivered/failed counts plus the updated <see cref="EmailRecipient"/> entities
    /// (with DeliveredAt/FailedAt populated). Callers are responsible for persisting
    /// the updated recipients and final email/batch status.
    /// </summary>
    private async Task<(int delivered, int failed, List<EmailRecipient> updated)> SendCohortAsync(
        List<EmailRecipient> recipients,
        string subject,
        string renderedHtml,
        string fromAddress,
        string? replyTo,
        Email email,
        string siteUrl,
        string newsletterId,
        CancellationToken ct)
    {
        if (recipients.Count == 0)
            return (0, 0, new List<EmailRecipient>());

        var batchRequest = new EmailBatchRequest
        {
            From = fromAddress,
            ReplyTo = replyTo,
            Subject = subject,
            Html = renderedHtml,
            Plaintext = email.Plaintext ?? string.Empty,
            Recipients = BuildRecipientSubstitutions(recipients, email.Id, siteUrl, newsletterId),
        };

        var results = await emailService.SendBatchAsync(batchRequest, ct);

        var delivered = 0;
        var failed = 0;
        var updated = new List<EmailRecipient>();
        foreach (var result in results)
        {
            var recipient = recipients.FirstOrDefault(r => r.MemberEmail == result.RecipientEmail);
            if (recipient is null) continue;

            if (result.Success)
            {
                recipient.DeliveredAt = DateTime.UtcNow;
                delivered++;
            }
            else
            {
                recipient.FailedAt = DateTime.UtcNow;
                failed++;
                recipient.Failures.Add(new EmailRecipientFailure
                {
                    Id = GenerateId(),
                    EmailId = email.Id,
                    MemberId = recipient.MemberId,
                    EmailRecipientId = recipient.Id,
                    Code = result.ErrorCode ?? 0,
                    Message = result.ErrorMessage ?? "Unknown error",
                    Severity = "permanent",
                    FailedAt = DateTime.UtcNow,
                });
            }
            updated.Add(recipient);
        }

        return (delivered, failed, updated);
    }

    /// <summary>
    /// Deterministically partitions subscribers into variant-A, variant-B, and
    /// holdout cohorts for A/B subject-line testing. Ordering is stable across reruns
    /// (sorted by member Uuid) so the assignment is reproducible.
    /// </summary>
    private static (List<Member> cohortA, List<Member> cohortB, List<Member> holdout)
        PartitionForAbTest(List<Member> subscribers, int splitPercent)
    {
        var ordered = subscribers.OrderBy(m => m.Uuid, StringComparer.Ordinal).ToList();
        var perCohort = Math.Max(1, ordered.Count * splitPercent / 100);
        var cohortA = ordered.Take(perCohort).ToList();
        var cohortB = ordered.Skip(perCohort).Take(perCohort).ToList();
        var holdout = ordered.Skip(perCohort * 2).ToList();
        return (cohortA, cohortB, holdout);
    }

    public async Task<Email> SendAbTestWinnerAsync(string emailId, string siteUrl, CancellationToken ct = default)
    {
        var email = await emailRepository.GetByIdAsync(emailId, ct)
            ?? throw new InvalidOperationException($"Email '{emailId}' not found.");

        if (email.AbTestPhase != "testing")
            return email; // No-op when not in testing phase

        if (string.IsNullOrWhiteSpace(email.SubjectB))
            throw new InvalidOperationException($"Email '{emailId}' has no SubjectB recorded.");

        siteUrl = siteUrl.TrimEnd('/');

        // Compute opens per variant from EmailRecipient (authoritative; ignores Email.OpenedCount drift)
        var opens = await emailRepository.GetAbVariantOpenCountsAsync(emailId, ct);
        opens.TryGetValue("a", out var opensA);
        opens.TryGetValue("b", out var opensB);

        // Tie-breaker: variant A wins ties (it's the originally configured subject).
        var winner = opensB > opensA ? "b" : "a";
        var winningSubject = winner == "a" ? email.Subject! : email.SubjectB!;

        var holdoutRecipients = await emailRepository.GetHoldoutRecipientsAsync(emailId, ct);

        // If there are no holdout recipients, just record the winner and close the test.
        if (holdoutRecipients.Count == 0)
        {
            email.AbTestPhase = "completed";
            email.AbTestWinnerVariant = winner;
            email.AbTestOpensA = opensA;
            email.AbTestOpensB = opensB;
            if (winner == "b") email.Subject = email.SubjectB;
            email.Status = email.FailedCount > 0 && email.DeliveredCount == 0 ? "failed" : "submitted";
            email.UpdatedAt = DateTime.UtcNow;
            await emailRepository.UpdateEmailAsync(email, ct);
            return email;
        }

        var newsletterId = email.NewsletterId
            ?? throw new InvalidOperationException($"Email '{emailId}' has no newsletter assigned.");
        var newsletter = await newsletterRepository.GetByIdAsync(newsletterId, ct)
            ?? throw new InvalidOperationException($"Newsletter '{newsletterId}' not found for email '{emailId}'.");
        var post = await postRepository.GetByIdAsync(email.PostId, ct)
            ?? throw new InvalidOperationException($"Post '{email.PostId}' not found for email '{emailId}'.");

        // Re-render template (tracked HTML is stored on Email already; reuse it)
        var siteTitle = await settingsService.GetStringAsync("title", ct);
        var siteIconUrl = await settingsService.GetStringAsync("icon", ct);
        var accentColor = await settingsService.GetStringAsync("accent_color", ct);
        string? authorName = post.PostsAuthors?.OrderBy(pa => pa.SortOrder).FirstOrDefault()?.Author?.Name;
        var templateModel = BuildTemplateModel(post, newsletter, siteUrl, siteTitle, siteIconUrl, accentColor, authorName);
        var renderedTemplate = await emailService.RenderTemplateAsync("newsletter", templateModel, ct);

        // Process holdout in batches of 500, mirroring ProcessPendingEmailAsync pattern.
        const int batchSize = 500;
        var totalDelivered = 0;
        var totalFailed = 0;

        for (var offset = 0; offset < holdoutRecipients.Count; offset += batchSize)
        {
            var chunk = holdoutRecipients.Skip(offset).Take(batchSize).ToList();

            EmailBatch batch;
            await using (var batchSetupTxn = await emailRepository.BeginTransactionAsync(ct))
            {
                batch = new EmailBatch
                {
                    Id = GenerateId(),
                    EmailId = email.Id,
                    Status = "submitting",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                await emailRepository.AddBatchAsync(batch, ct);

                foreach (var r in chunk)
                {
                    r.BatchId = batch.Id;
                    r.ProcessedAt = DateTime.UtcNow;
                }
                await emailRepository.UpdateRecipientsAsync(chunk, ct);
                await batchSetupTxn.CommitAsync(ct);
            }

            var (delivered, failed, updated) = await SendCohortAsync(
                chunk, winningSubject, renderedTemplate, email.From!, email.ReplyTo,
                email, siteUrl, newsletterId, ct);

            await using (var batchStatusTxn = await emailRepository.BeginTransactionAsync(ct))
            {
                if (updated.Count > 0)
                    await emailRepository.UpdateRecipientsAsync(updated, ct);

                batch.Status = failed == chunk.Count ? "failed" : "submitted";
                batch.UpdatedAt = DateTime.UtcNow;
                await emailRepository.UpdateBatchAsync(batch, ct);

                await batchStatusTxn.CommitAsync(ct);
            }

            totalDelivered += delivered;
            totalFailed += failed;
        }

        email.AbTestPhase = "completed";
        email.AbTestWinnerVariant = winner;
        email.AbTestOpensA = opensA;
        email.AbTestOpensB = opensB;
        if (winner == "b") email.Subject = email.SubjectB;
        email.DeliveredCount += totalDelivered;
        email.FailedCount += totalFailed;
        email.Status = email.FailedCount == email.EmailCount ? "failed" : "submitted";
        email.UpdatedAt = DateTime.UtcNow;
        await emailRepository.UpdateEmailAsync(email, ct);
        return email;
    }

    /// <summary>
    /// Generates a 24-character lowercase hex ID (matches Ghost's ObjectId format).
    /// </summary>
    private static string GenerateId()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [GeneratedRegex("""href="([^"]+)""" + "\"", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
