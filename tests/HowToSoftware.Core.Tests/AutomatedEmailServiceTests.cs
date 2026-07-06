using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Core.Tests;

public class AutomatedEmailServiceTests
{
    private readonly FakeAutomatedEmailRepo _repo = new();
    private readonly FakeEmailRepo _emailRepo = new();
    private readonly FakeEmailService _emailService = new();
    private readonly FakeSettingsService _settingsService = new();
    private readonly FakeLexicalRenderer _lexicalRenderer = new();
    private readonly AutomatedEmailService _sut;

    public AutomatedEmailServiceTests()
    {
        _sut = new AutomatedEmailService(
            _repo,
            _emailRepo,
            _emailService,
            _settingsService,
            _lexicalRenderer,
            NullLogger<AutomatedEmailService>.Instance);
    }

    // ── SendAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ActiveEmail_SendsToMember()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "active"));

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Single(_emailService.SentMessages);
        Assert.Equal("alice@example.com", _emailService.SentMessages[0].To);
        Assert.Single(_repo.Recipients);
        Assert.Equal("m1", _repo.Recipients[0].MemberId);
    }

    [Fact]
    public async Task SendAsync_InactiveEmail_DoesNotSend()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "inactive"));

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Empty(_emailService.SentMessages);
        Assert.Empty(_repo.Recipients);
    }

    [Fact]
    public async Task SendAsync_UnknownSlug_DoesNotSend()
    {
        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("nonexistent", member, "https://example.com");

        Assert.Empty(_emailService.SentMessages);
    }

    [Fact]
    public async Task SendAsync_AlreadySent_DoesNotSendAgain()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "active"));
        _repo.Recipients.Add(new AutomatedEmailRecipient
        {
            Id = "r1",
            AutomatedEmailId = "ae1",
            MemberId = "m1",
            MemberUuid = "uuid1",
            MemberEmail = "alice@example.com",
            CreatedAt = DateTime.UtcNow,
        });

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Empty(_emailService.SentMessages);
        Assert.Single(_repo.Recipients); // no new recipient
    }

    [Fact]
    public async Task SendAsync_SuppressedEmail_DoesNotSend()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "active"));
        _emailRepo.SuppressedEmails.Add("alice@example.com");

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Empty(_emailService.SentMessages);
    }

    [Fact]
    public async Task SendAsync_SubstitutesSubjectPlaceholder()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "active", subject: "\U0001f44b Welcome to {{site_title}}!"));

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Single(_emailService.SentMessages);
        Assert.Equal("\U0001f44b Welcome to TestSite!", _emailService.SentMessages[0].Subject);
    }

    [Fact]
    public async Task SendAsync_RendersLexicalContent()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.Lexical = "{\"root\":{\"children\":[{\"children\":[{\"type\":\"text\",\"text\":\"Hello\"}],\"type\":\"paragraph\"}],\"type\":\"root\"}}";
        _repo.Emails.Add(ae);

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Single(_lexicalRenderer.RenderedInputs);
        Assert.Equal(ae.Lexical, _lexicalRenderer.RenderedInputs[0]);
    }

    [Fact]
    public async Task SendAsync_FailedSend_RecordsRecipientWithFailure()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "active"));
        _emailService.ShouldFail = true;

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Single(_emailService.SentMessages);
        var recipient = Assert.Single(_repo.Recipients);
        Assert.NotNull(recipient.FailedAt);
        Assert.Null(recipient.DeliveredAt);
    }

    [Fact]
    public async Task SendAsync_UsesSenderFieldsFromAutomatedEmail()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.SenderName = "Custom Sender";
        ae.SenderEmail = "custom@example.com";
        ae.SenderReplyTo = "reply@example.com";
        _repo.Emails.Add(ae);

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        var sent = Assert.Single(_emailService.SentMessages);
        Assert.Equal("Custom Sender <custom@example.com>", sent.From);
        Assert.Equal("reply@example.com", sent.ReplyTo);
    }

    // ── CRUD ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SetsIdAndCreatedAt()
    {
        var ae = new AutomatedEmail
        {
            Name = "Test",
            Slug = "test",
            Subject = "Test Subject",
        };

        var created = await _sut.CreateAsync(ae);

        Assert.NotNull(created.Id);
        Assert.Equal(24, created.Id.Length);
        Assert.True(created.CreatedAt > DateTime.MinValue);
        Assert.Single(_repo.Emails);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var ae = MakeAutomatedEmail("ae1", "test", "active");
        _repo.Emails.Add(ae);

        await _sut.UpdateAsync(ae);

        Assert.NotNull(ae.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEmail()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "test", "active"));

        await _sut.DeleteAsync("ae1");

        Assert.Empty(_repo.Emails);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        _repo.Emails.Add(MakeAutomatedEmail("ae1", "welcome", "active"));
        _repo.Emails.Add(MakeAutomatedEmail("ae2", "activation", "inactive"));

        var all = await _sut.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    // ── Drip / sequence (AUTO.4) ───────────────────────────────

    [Fact]
    public async Task SendAsync_WithDelay_EnqueuesScheduleInsteadOfSending()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.DelayMinutes = 60 * 24 * 3; // 3 days
        _repo.Emails.Add(ae);

        var member = MakeMember("m1", "alice@example.com");
        var before = DateTime.UtcNow;
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Empty(_emailService.SentMessages);
        Assert.Empty(_repo.Recipients);
        var schedule = Assert.Single(_repo.Schedules);
        Assert.Equal("ae1", schedule.AutomatedEmailId);
        Assert.Equal("m1", schedule.MemberId);
        Assert.Equal("alice@example.com", schedule.MemberEmail);
        Assert.Equal("https://example.com", schedule.SiteUrl);
        // ScheduledFor ~= now + 3 days (allow a few seconds drift).
        Assert.InRange(schedule.ScheduledFor,
            before.AddDays(3).AddSeconds(-5),
            DateTime.UtcNow.AddDays(3).AddSeconds(5));
        Assert.Null(schedule.ProcessedAt);
    }

    [Fact]
    public async Task SendAsync_TriggerEventFansOutToMultipleEmails()
    {
        // Three-step drip sequence keyed on trigger="signup".
        var ae1 = MakeAutomatedEmail("ae1", "welcome", "active");
        ae1.TriggerEvent = "signup";
        ae1.DelayMinutes = 0; // sends immediately
        var ae2 = MakeAutomatedEmail("ae2", "day3-tips", "active");
        ae2.TriggerEvent = "signup";
        ae2.DelayMinutes = 60 * 24 * 3;
        var ae3 = MakeAutomatedEmail("ae3", "day7-recap", "active");
        ae3.TriggerEvent = "signup";
        ae3.DelayMinutes = 60 * 24 * 7;
        _repo.Emails.Add(ae1);
        _repo.Emails.Add(ae2);
        _repo.Emails.Add(ae3);

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("signup", member, "https://example.com");

        // ae1 sent immediately, ae2+ae3 queued.
        Assert.Single(_emailService.SentMessages);
        Assert.Single(_repo.Recipients);
        Assert.Equal("ae1", _repo.Recipients[0].AutomatedEmailId);
        Assert.Equal(2, _repo.Schedules.Count);
        Assert.Contains(_repo.Schedules, s => s.AutomatedEmailId == "ae2");
        Assert.Contains(_repo.Schedules, s => s.AutomatedEmailId == "ae3");
    }

    [Fact]
    public async Task SendAsync_DoesNotDoubleScheduleSameMember()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.DelayMinutes = 60;
        _repo.Emails.Add(ae);

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");

        Assert.Single(_repo.Schedules);
    }

    [Fact]
    public async Task DispatchScheduledAsync_SendsAndMarksProcessed()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.DelayMinutes = 60;
        _repo.Emails.Add(ae);

        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");
        var schedule = _repo.Schedules.Single();

        await _sut.DispatchScheduledAsync(schedule.Id);

        var sent = Assert.Single(_emailService.SentMessages);
        Assert.Equal("alice@example.com", sent.To);
        var recipient = Assert.Single(_repo.Recipients);
        Assert.Equal("m1", recipient.MemberId);
        Assert.NotNull(schedule.ProcessedAt);
        Assert.Null(schedule.FailureReason);
    }

    [Fact]
    public async Task DispatchScheduledAsync_AutomatedEmailDeactivated_MarksProcessedWithoutSending()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.DelayMinutes = 60;
        _repo.Emails.Add(ae);
        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");
        var schedule = _repo.Schedules.Single();

        // Deactivate between schedule and dispatch.
        ae.Status = "inactive";

        await _sut.DispatchScheduledAsync(schedule.Id);

        Assert.Empty(_emailService.SentMessages);
        Assert.Empty(_repo.Recipients);
        Assert.NotNull(schedule.ProcessedAt);
        Assert.Contains("inactive", schedule.FailureReason);
    }

    [Fact]
    public async Task DispatchScheduledAsync_SuppressedRecipient_MarksProcessedWithoutSending()
    {
        var ae = MakeAutomatedEmail("ae1", "welcome", "active");
        ae.DelayMinutes = 60;
        _repo.Emails.Add(ae);
        var member = MakeMember("m1", "alice@example.com");
        await _sut.SendAsync("welcome", member, "https://example.com");
        var schedule = _repo.Schedules.Single();

        _emailRepo.SuppressedEmails.Add("alice@example.com");

        await _sut.DispatchScheduledAsync(schedule.Id);

        Assert.Empty(_emailService.SentMessages);
        Assert.NotNull(schedule.ProcessedAt);
        Assert.Equal("Recipient address is suppressed", schedule.FailureReason);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static AutomatedEmail MakeAutomatedEmail(string id, string slug, string status, string? subject = null) => new()
    {
        Id = id,
        Slug = slug,
        Name = $"Email {slug}",
        Subject = subject ?? $"Subject for {slug}",
        Status = status,
        CreatedAt = DateTime.UtcNow,
    };

    private static Member MakeMember(string id, string email) => new()
    {
        Id = id,
        Uuid = $"uuid-{id}",
        TransientId = $"transient-{id}",
        Email = email,
        Status = "free",
        CreatedAt = DateTime.UtcNow,
    };

    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakeAutomatedEmailRepo : IAutomatedEmailRepository
    {
        public List<AutomatedEmail> Emails { get; } = [];
        public List<AutomatedEmailRecipient> Recipients { get; } = [];
        public List<AutomatedEmailSchedule> Schedules { get; } = [];

        public Task<AutomatedEmail?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Emails.FirstOrDefault(e => e.Id == id));
        public Task<AutomatedEmail?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Emails.FirstOrDefault(e => e.Slug == slug));
        public Task<List<AutomatedEmail>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Emails.ToList());
        public Task AddAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
        { Emails.Add(automatedEmail); return Task.CompletedTask; }
        public Task UpdateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Emails.RemoveAll(e => e.Id == id); return Task.CompletedTask; }
        public Task<bool> HasRecipientAsync(string automatedEmailId, string memberId, CancellationToken ct = default)
            => Task.FromResult(Recipients.Any(r => r.AutomatedEmailId == automatedEmailId && r.MemberId == memberId));
        public Task AddRecipientAsync(AutomatedEmailRecipient recipient, CancellationToken ct = default)
        { Recipients.Add(recipient); return Task.CompletedTask; }
        public Task<int> GetRecipientCountAsync(string automatedEmailId, CancellationToken ct = default)
            => Task.FromResult(Recipients.Count(r => r.AutomatedEmailId == automatedEmailId));

        public Task<AutomatedEmailStatistics> GetStatisticsAsync(string automatedEmailId, CancellationToken ct = default)
        {
            var rows = Recipients.Where(r => r.AutomatedEmailId == automatedEmailId).ToList();
            return Task.FromResult(new AutomatedEmailStatistics(
                rows.Count,
                rows.Count(r => r.DeliveredAt != null),
                rows.Count(r => r.OpenedAt != null),
                rows.Count(r => r.ClickedAt != null),
                rows.Count(r => r.FailedAt != null),
                rows.Count(r => r.BouncedAt != null)));
        }

        public Task<Dictionary<string, AutomatedEmailStatistics>> GetStatisticsBatchAsync(
            IEnumerable<string> automatedEmailIds, CancellationToken ct = default)
        {
            var dict = new Dictionary<string, AutomatedEmailStatistics>();
            foreach (var id in automatedEmailIds.Distinct())
            {
                var rows = Recipients.Where(r => r.AutomatedEmailId == id).ToList();
                dict[id] = new AutomatedEmailStatistics(
                    rows.Count,
                    rows.Count(r => r.DeliveredAt != null),
                    rows.Count(r => r.OpenedAt != null),
                    rows.Count(r => r.ClickedAt != null),
                    rows.Count(r => r.FailedAt != null),
                    rows.Count(r => r.BouncedAt != null));
            }
            return Task.FromResult(dict);
        }

        public Task<PagedResult<AutomatedEmailRecipient>> GetRecipientsAsync(
            string automatedEmailId, int page, int pageSize, CancellationToken ct = default)
        {
            var all = Recipients.Where(r => r.AutomatedEmailId == automatedEmailId)
                .OrderByDescending(r => r.CreatedAt).ToList();
            return Task.FromResult(new PagedResult<AutomatedEmailRecipient>
            {
                Items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = all.Count,
            });
        }

        public Task<string?> MarkRecipientEventAsync(
            string email, string eventType, DateTime occurredAt, string? failureReason, CancellationToken ct = default)
        {
            var r = Recipients.Where(x => x.MemberEmail == email)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (r is null) return Task.FromResult<string?>(null);
            switch (eventType)
            {
                case "delivered": r.DeliveredAt ??= occurredAt; break;
                case "opened": r.OpenedAt ??= occurredAt; r.DeliveredAt ??= occurredAt; break;
                case "clicked": r.ClickedAt ??= occurredAt; r.DeliveredAt ??= occurredAt; break;
                case "failed": r.FailedAt ??= occurredAt; r.FailureReason ??= failureReason; break;
                case "bounced": r.BouncedAt ??= occurredAt; r.FailureReason ??= failureReason; break;
                default: return Task.FromResult<string?>(null);
            }
            return Task.FromResult<string?>(r.Id);
        }

        public Task<List<AutomatedEmail>> GetActiveByTriggerAsync(string trigger, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(trigger))
                return Task.FromResult(new List<AutomatedEmail>());
            return Task.FromResult(Emails
                .Where(e => e.Status == "active" && (e.Slug == trigger || e.TriggerEvent == trigger))
                .ToList());
        }

        public Task<bool> HasSentOrScheduledAsync(string automatedEmailId, string memberId, CancellationToken ct = default)
        {
            var hasRecipient = Recipients.Any(r => r.AutomatedEmailId == automatedEmailId && r.MemberId == memberId);
            if (hasRecipient) return Task.FromResult(true);
            return Task.FromResult(Schedules.Any(s => s.AutomatedEmailId == automatedEmailId
                                                   && s.MemberId == memberId
                                                   && s.ProcessedAt == null));
        }

        public Task AddScheduleAsync(AutomatedEmailSchedule schedule, CancellationToken ct = default)
        { Schedules.Add(schedule); return Task.CompletedTask; }

        public Task<List<AutomatedEmailSchedule>> GetDueSchedulesAsync(DateTime now, int limit, CancellationToken ct = default)
            => Task.FromResult(Schedules
                .Where(s => s.ProcessedAt == null && s.ScheduledFor <= now)
                .OrderBy(s => s.ScheduledFor)
                .Take(limit)
                .ToList());

        public Task<AutomatedEmailSchedule?> GetScheduleByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Schedules.FirstOrDefault(s => s.Id == id));

        public Task MarkScheduleProcessedAsync(string id, DateTime processedAt, string? failureReason, CancellationToken ct = default)
        {
            var row = Schedules.FirstOrDefault(s => s.Id == id);
            if (row is null) return Task.CompletedTask;
            row.ProcessedAt = processedAt;
            row.FailureReason = failureReason;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailRepo : IEmailRepository
    {
        public List<string> SuppressedEmails { get; } = [];

        public Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
            => Task.FromResult<ITransactionScope>(new NoOpTransactionScope());

        public Task<bool> IsEmailSuppressedAsync(string email, CancellationToken ct = default)
            => Task.FromResult(SuppressedEmails.Contains(email));

        // Unused methods for this test
        public Task<Email?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Email?>(null);
        public Task<List<Email>> GetPendingEmailsAsync(CancellationToken ct = default) => Task.FromResult(new List<Email>());
        public Task<List<Email>> GetAbTestsAwaitingWinnerAsync(DateTime now, CancellationToken ct = default) => Task.FromResult(new List<Email>());
        public Task<List<EmailRecipient>> GetHoldoutRecipientsAsync(string emailId, CancellationToken ct = default) => Task.FromResult(new List<EmailRecipient>());
        public Task<Dictionary<string, int>> GetAbVariantOpenCountsAsync(string emailId, CancellationToken ct = default) => Task.FromResult(new Dictionary<string, int>());
        public Task AddEmailAsync(Email email, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateEmailAsync(Email email, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddBatchAsync(EmailBatch batch, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateBatchAsync(EmailBatch batch, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default) => Task.CompletedTask;
        public Task<EmailRecipient?> GetRecipientByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<EmailRecipient?>(null);
        public Task UpdateRecipientAsync(EmailRecipient recipient, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddRedirectAsync(Redirect redirect, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Redirect?> GetRedirectByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Redirect?>(null);
        public Task AddClickEventAsync(MembersClickEvent clickEvent, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<string>> GetSuppressedEmailsAsync(CancellationToken ct = default) => Task.FromResult(SuppressedEmails.ToList());
        public Task AddSuppressionAsync(Suppression suppression, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveSuppressionAsync(string email, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddSpamComplaintEventAsync(EmailSpamComplaintEvent evt, CancellationToken ct = default) => Task.CompletedTask;
        public Task<MembersNewsletter?> GetSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default) => Task.FromResult<MembersNewsletter?>(null);
        public Task<List<MembersNewsletter>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default) => Task.FromResult(new List<MembersNewsletter>());
        public Task AddSubscriptionAsync(MembersNewsletter subscription, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<MembersFeedback?> GetFeedbackAsync(string memberId, string postId, CancellationToken ct = default) => Task.FromResult<MembersFeedback?>(null);
        public Task AddFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];
        public bool ShouldFail { get; set; }

        public Task<string> RenderTemplateAsync(string templateName, EmailTemplateModel model, CancellationToken ct = default)
            => Task.FromResult("<html>rendered</html>");
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            SentMessages.Add(message);
            return Task.FromResult(new EmailSendResult
            {
                RecipientEmail = message.To,
                Success = !ShouldFail,
                ErrorMessage = ShouldFail ? "Simulated failure" : null,
            });
        }
        public Task<List<EmailSendResult>> SendBatchAsync(EmailBatchRequest request, CancellationToken ct = default)
            => Task.FromResult(new List<EmailSendResult>());
    }

    private sealed class FakeLexicalRenderer : ILexicalRenderer
    {
        public List<string> RenderedInputs { get; } = [];

        public string Render(string lexicalJson)
        {
            RenderedInputs.Add(lexicalJson);
            return "<p>Rendered content</p>";
        }
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public Task<string?> GetStringAsync(string key, CancellationToken ct = default)
            => Task.FromResult(key == "title" ? "TestSite" : (string?)null);
        public Task<bool?> GetBoolAsync(string key, CancellationToken ct = default)
            => Task.FromResult<bool?>(null);
        public Task<int?> GetIntAsync(string key, CancellationToken ct = default)
            => Task.FromResult<int?>(null);
        public Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);
        public Task<Setting?> GetAsync(string key, CancellationToken ct = default)
            => Task.FromResult<Setting?>(null);
        public Task<List<Setting>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<Setting>());
        public Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default)
            => Task.FromResult(new List<Setting>());
        public Task SetAsync(string key, string? value, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task SetBoolAsync(string key, bool value, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task SetIntAsync(string key, int value, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task SetJsonAsync<T>(string key, T value, CancellationToken ct = default) where T : class
            => Task.CompletedTask;
        public Task DeleteAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
        public void InvalidateCache() { }
        public void InvalidateCache(string key) { }
    }

    private sealed class NoOpTransactionScope : ITransactionScope
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
