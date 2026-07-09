using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Core.Services;

public sealed class AutomatedEmailService : IAutomatedEmailService
{
    private readonly IAutomatedEmailRepository _repo;
    private readonly IEmailRepository _emailRepo;
    private readonly IEmailService _email;
    private readonly ISettingsService _settings;
    private readonly ILexicalRenderer _lexical;
    private readonly ILogger<AutomatedEmailService> _logger;

    public AutomatedEmailService(
        IAutomatedEmailRepository repo,
        IEmailRepository emailRepo,
        IEmailService email,
        ISettingsService settings,
        ILexicalRenderer lexical,
        ILogger<AutomatedEmailService> logger)
    {
        _repo = repo;
        _emailRepo = emailRepo;
        _email = email;
        _settings = settings;
        _lexical = lexical;
        _logger = logger;
    }

    public async Task SendAsync(string slug, Member member, string siteUrl, CancellationToken ct = default)
    {
        // Fan out: a trigger string may match a single email by slug AND/OR
        // many emails by trigger_event (drip sequence). De-dup by id.
        var targets = await _repo.GetActiveByTriggerAsync(slug, ct);

        // Legacy fallback: when the slug matches an INACTIVE email but no
        // active trigger entries, surface the same "not active" log as before
        // so existing tests/observability stay consistent.
        if (targets.Count == 0)
        {
            var fallback = await _repo.GetBySlugAsync(slug, ct);
            if (fallback is null)
            {
                _logger.LogWarning("Automated email with slug '{Slug}' not found", slug);
                return;
            }
            // Fallback is non-null but Status != "active" (otherwise it would
            // be in `targets`). Log and exit.
            _logger.LogDebug("Automated email '{Slug}' is not active (status={Status})", slug, fallback.Status);
            return;
        }

        // Cheap upfront suppression check — covers every fan-out target. If
        // the address is suppressed we don't need to schedule anything: the
        // dispatcher would just no-op later. Re-checked inside SendImmediate
        // for the moment of actual send (state can change between schedule
        // and dispatch).
        if (await _emailRepo.IsEmailSuppressedAsync(member.Email, ct))
        {
            _logger.LogDebug("Member {MemberId} is suppressed, skipping automated email '{Slug}'",
                member.Id, LogSanitizer.SanitizeForLog(slug));
            return;
        }

        foreach (var target in targets.DistinctBy(t => t.Id))
        {
            // Dedup across both already-sent recipients AND pending schedule rows.
            if (await _repo.HasSentOrScheduledAsync(target.Id, member.Id, ct))
            {
                _logger.LogDebug("Member {MemberId} already received or is queued for automated email '{Slug}'",
                    member.Id, target.Slug);
                continue;
            }

            if (target.DelayMinutes <= 0)
            {
                await SendImmediateAsync(target, member, siteUrl, ct);
            }
            else
            {
                var scheduledFor = DateTime.UtcNow.AddMinutes(target.DelayMinutes);
                await _repo.AddScheduleAsync(new AutomatedEmailSchedule
                {
                    Id = GenerateId(),
                    AutomatedEmailId = target.Id,
                    MemberId = member.Id,
                    MemberUuid = member.Uuid ?? member.Id,
                    MemberEmail = member.Email,
                    MemberName = member.Name,
                    SiteUrl = siteUrl,
                    ScheduledFor = scheduledFor,
                    CreatedAt = DateTime.UtcNow,
                }, ct);
                _logger.LogInformation(
                    "Scheduled automated email '{Slug}' for member {MemberId} at {ScheduledFor:o} (delay={DelayMinutes}m)",
                    LogSanitizer.SanitizeForLog(target.Slug), member.Id, scheduledFor, target.DelayMinutes);
            }
        }
    }

    public async Task DispatchScheduledAsync(string scheduleId, CancellationToken ct = default)
    {
        var schedule = await _repo.GetScheduleByIdAsync(scheduleId, ct);
        if (schedule is null || schedule.ProcessedAt is not null)
            return;

        var automatedEmail = await _repo.GetByIdAsync(schedule.AutomatedEmailId, ct);
        if (automatedEmail is null)
        {
            await _repo.MarkScheduleProcessedAsync(scheduleId, DateTime.UtcNow,
                "Automated email no longer exists", ct);
            return;
        }

        // Quietly drop the queued send if the email has been deactivated since
        // the schedule was created.
        if (automatedEmail.Status != "active")
        {
            await _repo.MarkScheduleProcessedAsync(scheduleId, DateTime.UtcNow,
                $"Automated email status is '{automatedEmail.Status}' at dispatch time", ct);
            return;
        }

        if (await _emailRepo.IsEmailSuppressedAsync(schedule.MemberEmail, ct))
        {
            await _repo.MarkScheduleProcessedAsync(scheduleId, DateTime.UtcNow,
                "Recipient address is suppressed", ct);
            return;
        }

        // Late dedup: if another path (e.g. an immediate send for a different
        // trigger fan-out) already delivered this automated email to the
        // member, skip and mark processed.
        if (await _repo.HasRecipientAsync(automatedEmail.Id, schedule.MemberId, ct))
        {
            await _repo.MarkScheduleProcessedAsync(scheduleId, DateTime.UtcNow,
                "Already delivered through another path", ct);
            return;
        }

        // Reconstruct the minimal Member projection that SendImmediateAsync
        // needs. We snapshot email/uuid/name at enqueue time so a later
        // member rename or email change doesn't silently retarget the send.
        var memberSnapshot = new Member
        {
            Id = schedule.MemberId,
            Uuid = schedule.MemberUuid,
            Email = schedule.MemberEmail,
            Name = schedule.MemberName,
        };

        try
        {
            await SendImmediateAsync(automatedEmail, memberSnapshot, schedule.SiteUrl, ct);
            await _repo.MarkScheduleProcessedAsync(scheduleId, DateTime.UtcNow, null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to dispatch scheduled automated email '{Slug}' for member {MemberId} (schedule={ScheduleId})",
                automatedEmail.Slug, schedule.MemberId, scheduleId);
            await _repo.MarkScheduleProcessedAsync(scheduleId, DateTime.UtcNow, ex.Message, ct);
        }
    }

    private async Task SendImmediateAsync(AutomatedEmail automatedEmail, Member member, string siteUrl, CancellationToken ct)
    {
        var slug = automatedEmail.Slug;
        // Build the email content
        var siteTitle = await _settings.GetStringAsync("title", ct) ?? "HowToSoftware";
        var siteIcon = await _settings.GetStringAsync("icon", ct);
        var accentColor = await _settings.GetStringAsync("accent_color", ct) ?? "#FF1A75";

        var senderName = automatedEmail.SenderName ?? siteTitle;
        var senderEmail = automatedEmail.SenderEmail
                          ?? await _settings.GetStringAsync("members_support_address", ct)
                          ?? await BuildDefaultSenderAsync(siteUrl, ct);
        var replyTo = automatedEmail.SenderReplyTo;

        // Render body from Lexical JSON, or fall back to empty body
        string htmlBody = string.Empty;
        if (!string.IsNullOrWhiteSpace(automatedEmail.Lexical))
        {
            htmlBody = _lexical.Render(automatedEmail.Lexical);
        }

        // Use the "automated" email template
        var model = new EmailTemplateModel
        {
            SiteTitle = siteTitle,
            SiteUrl = siteUrl,
            SiteIconUrl = siteIcon is not null ? $"{siteUrl.TrimEnd('/')}/{siteIcon.TrimStart('/')}" : null,
            AccentColor = accentColor,
            PostTitle = automatedEmail.Name,
            HtmlBody = htmlBody,
            ShowHeaderIcon = siteIcon is not null,
            ShowHeaderTitle = true,
            ShowHeaderName = false,
            ShowPostTitleSection = true,
            ShowFeatureImage = false,
            ShowExcerpt = false,
            ShowCommentCta = false,
            ShowBadge = false,
            FeedbackEnabled = false,
            BackgroundColor = "light",
            TitleFontCategory = "sans_serif",
            TitleAlignment = "center",
            TitleFontWeight = "bold",
            BodyFontCategory = "sans_serif",
            ButtonCorners = "rounded",
            ButtonStyle = "fill",
            ButtonColor = "accent",
            LinkStyle = "underline",
            LinkColor = "accent",
            ImageCorners = "square",
            HeaderBackgroundColor = "transparent",
            UnsubscribeUrl = $"{siteUrl.TrimEnd('/')}/#/portal/account",
        };

        var html = await _email.RenderTemplateAsync("automated", model, ct);

        // Build plaintext fallback
        var plaintext = BuildPlaintext(automatedEmail.Name, siteTitle, siteUrl);

        var result = await _email.SendAsync(new EmailMessage
        {
            From = $"{senderName} <{senderEmail}>",
            ReplyTo = replyTo,
            To = member.Email,
            Subject = automatedEmail.Subject.Replace("{{site_title}}", siteTitle, StringComparison.OrdinalIgnoreCase),
            Html = html,
            Plaintext = plaintext,
        }, ct);

        if (result.Success)
        {
            // Record that this member received this automated email
            await _repo.AddRecipientAsync(new AutomatedEmailRecipient
            {
                Id = GenerateId(),
                AutomatedEmailId = automatedEmail.Id,
                MemberId = member.Id,
                MemberUuid = member.Uuid ?? member.Id,
                MemberEmail = member.Email,
                MemberName = member.Name,
                CreatedAt = DateTime.UtcNow,
            }, ct);

            _logger.LogInformation("Sent automated email '{Slug}' to member {MemberId}",
                LogSanitizer.SanitizeForLog(slug), member.Id);
        }
        else
        {
            // Persist a recipient row with FailedAt so the failure is
            // visible in the per-email delivery statistics and the
            // recipient list. We still mark the recipient as having been
            // attempted so dedup (HasRecipientAsync) prevents an infinite
            // retry storm on the next trigger.
            await _repo.AddRecipientAsync(new AutomatedEmailRecipient
            {
                Id = GenerateId(),
                AutomatedEmailId = automatedEmail.Id,
                MemberId = member.Id,
                MemberUuid = member.Uuid ?? member.Id,
                MemberEmail = member.Email,
                MemberName = member.Name,
                CreatedAt = DateTime.UtcNow,
                FailedAt = DateTime.UtcNow,
                FailureReason = result.ErrorMessage,
            }, ct);

            _logger.LogError("Failed to send automated email '{Slug}' to member {MemberId}: {Error}",
                LogSanitizer.SanitizeForLog(slug), member.Id, LogSanitizer.SanitizeForLog(result.ErrorMessage));
        }
    }

    // ── CRUD ──

    public Task<AutomatedEmail?> GetByIdAsync(string id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<AutomatedEmail?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _repo.GetBySlugAsync(slug, ct);

    public Task<List<AutomatedEmail>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);

    public async Task<AutomatedEmail> CreateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
    {
        automatedEmail.Id = GenerateId();
        automatedEmail.CreatedAt = DateTime.UtcNow;
        await _repo.AddAsync(automatedEmail, ct);
        return automatedEmail;
    }

    public async Task UpdateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
    {
        automatedEmail.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(automatedEmail, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);

    public Task<int> GetRecipientCountAsync(string automatedEmailId, CancellationToken ct = default)
        => _repo.GetRecipientCountAsync(automatedEmailId, ct);

    public Task<AutomatedEmailStatistics> GetStatisticsAsync(string automatedEmailId, CancellationToken ct = default)
        => _repo.GetStatisticsAsync(automatedEmailId, ct);

    public Task<Dictionary<string, AutomatedEmailStatistics>> GetStatisticsBatchAsync(
        IEnumerable<string> automatedEmailIds, CancellationToken ct = default)
        => _repo.GetStatisticsBatchAsync(automatedEmailIds, ct);

    public Task<PagedResult<AutomatedEmailRecipient>> GetRecipientsAsync(
        string automatedEmailId, int page, int pageSize, CancellationToken ct = default)
        => _repo.GetRecipientsAsync(automatedEmailId, page, pageSize, ct);

    public Task<string?> RecordDeliveryEventAsync(
        string email, string eventType, DateTime occurredAt, string? failureReason, CancellationToken ct = default)
        => _repo.MarkRecipientEventAsync(email, eventType, occurredAt, failureReason, ct);

    // ── Helpers ──

    /// <summary>
    /// Builds a default sender address that aligns with the configured Mailgun
    /// sending domain (so SPF + DKIM pass). Falls back to the site host as a
    /// last resort \u2014 that path will likely spam-fold because the apex SPF
    /// usually does not authorize Mailgun.
    /// </summary>
    private async Task<string> BuildDefaultSenderAsync(string siteUrl, CancellationToken ct)
    {
        var mailgunDomain = await _settings.GetStringAsync("mailgun_domain", ct);
        if (!string.IsNullOrWhiteSpace(mailgunDomain)) return $"noreply@{mailgunDomain}";
        return $"noreply@{new Uri(siteUrl).Host}";
    }

    private static string BuildPlaintext(string name, string siteTitle, string siteUrl)
    {
        return $"{name}\n\n" +
               $"Visit {siteUrl} to browse our latest content.\n\n" +
               $"Sent from {siteTitle}";
    }

    private static string GenerateId()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
