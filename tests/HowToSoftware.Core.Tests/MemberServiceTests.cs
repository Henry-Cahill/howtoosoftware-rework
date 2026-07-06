using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Core.Tests;

public class MemberServiceTests
{
    private readonly FakeMemberRepo _memberRepo = new();
    private readonly FakeNewsletterRepo _newsletterRepo = new();
    private readonly FakeNewsletterService _newsletterService = new();
    private readonly FakeAutomatedEmailService _automatedEmailService = new();
    private readonly FakeSettingsService _settingsService = new();
    private readonly FakeWebhookDispatch _webhookDispatch = new();
    private readonly MemberService _sut;

    public MemberServiceTests()
    {
        _sut = new MemberService(
            _memberRepo,
            _newsletterRepo,
            _newsletterService,
            _automatedEmailService,
            _settingsService,
            _webhookDispatch,
            NullLogger<MemberService>.Instance);
    }

    // ── SignupAsync — happy path ────────────────────────────────

    [Fact]
    public async Task SignupAsync_NewMember_CreatesMemberAndReturnsIt()
    {
        _newsletterRepo.Active.Add(MakeNewsletter("nl1", subscribeOnSignup: true));

        var result = await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            Name = "Alice",
            SiteUrl = "https://example.com",
        });

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("free", result.Status);
        Assert.Single(_memberRepo.Members);
    }

    [Fact]
    public async Task SignupAsync_NewMember_AutoSubscribesToSignupNewsletters()
    {
        _newsletterRepo.Active.Add(MakeNewsletter("nl1", subscribeOnSignup: true));
        _newsletterRepo.Active.Add(MakeNewsletter("nl2", subscribeOnSignup: false));

        await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        // Should subscribe to nl1 only (SubscribeOnSignup = true)
        Assert.Single(_newsletterService.Subscriptions);
        Assert.Equal("nl1", _newsletterService.Subscriptions[0].NewsletterId);
    }

    [Fact]
    public async Task SignupAsync_WithExplicitNewsletterIds_SubscribesToThoseOnly()
    {
        _newsletterRepo.Active.Add(MakeNewsletter("nl1", subscribeOnSignup: true));
        _newsletterRepo.Active.Add(MakeNewsletter("nl2", subscribeOnSignup: false));

        await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            NewsletterIds = ["nl2"],
            SiteUrl = "https://example.com",
        });

        Assert.Single(_newsletterService.Subscriptions);
        Assert.Equal("nl2", _newsletterService.Subscriptions[0].NewsletterId);
    }

    [Fact]
    public async Task SignupAsync_NewMember_RecordsCreatedEvent()
    {
        _newsletterRepo.Active.Add(MakeNewsletter("nl1", subscribeOnSignup: true));

        var result = await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        Assert.Single(_memberRepo.CreatedEvents);
        Assert.Equal(result!.Id, _memberRepo.CreatedEvents[0].MemberId);
        Assert.Equal("member", _memberRepo.CreatedEvents[0].Source);
    }

    [Fact]
    public async Task SignupAsync_NewMember_RecordsStatusEvent()
    {
        await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        Assert.Single(_memberRepo.StatusEvents);
        Assert.Null(_memberRepo.StatusEvents[0].FromStatus);
        Assert.Equal("free", _memberRepo.StatusEvents[0].ToStatus);
    }

    [Fact]
    public async Task SignupAsync_NewMember_RecordsSubscribeEvents()
    {
        _newsletterRepo.Active.Add(MakeNewsletter("nl1", subscribeOnSignup: true));
        _newsletterRepo.Active.Add(MakeNewsletter("nl2", subscribeOnSignup: true));

        await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        Assert.Equal(2, _memberRepo.SubscribeEvents.Count);
        Assert.All(_memberRepo.SubscribeEvents, e => Assert.True(e.Subscribed));
    }

    [Fact]
    public async Task SignupAsync_NewMember_SendsWelcomeEmail()
    {
        await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        Assert.Single(_automatedEmailService.SentEmails);
        Assert.Equal("welcome", _automatedEmailService.SentEmails[0].Slug);
        Assert.Equal("test@example.com", _automatedEmailService.SentEmails[0].MemberEmail);
    }

    // ── SignupAsync — duplicate email ───────────────────────────

    [Fact]
    public async Task SignupAsync_DuplicateEmail_ReturnsNull()
    {
        _memberRepo.Members.Add(new Member
        {
            Id = "existing-id",
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = "test@example.com",
            Status = "free",
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        Assert.Null(result);
        Assert.Single(_memberRepo.Members); // no new member created
        Assert.Empty(_automatedEmailService.SentEmails); // no welcome email
    }

    // ── SignupAsync — no newsletters ────────────────────────────

    [Fact]
    public async Task SignupAsync_NoActiveNewsletters_StillCreatesMember()
    {
        var result = await _sut.SignupAsync(new MemberSignupRequest
        {
            Email = "test@example.com",
            SiteUrl = "https://example.com",
        });

        Assert.NotNull(result);
        Assert.Empty(_newsletterService.Subscriptions);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static Newsletter MakeNewsletter(string id, bool subscribeOnSignup) => new()
    {
        Id = id,
        Uuid = Guid.NewGuid().ToString("D"),
        Name = $"Newsletter {id}",
        Slug = id,
        Status = "active",
        Visibility = "members",
        SubscribeOnSignup = subscribeOnSignup,
        CreatedAt = DateTime.UtcNow,
    };

    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakeMemberRepo : IMemberRepository
    {
        public List<Member> Members { get; } = [];
        public List<MembersCreatedEvent> CreatedEvents { get; } = [];
        public List<MembersStatusEvent> StatusEvents { get; } = [];
        public List<MembersSubscribeEvent> SubscribeEvents { get; } = [];

        public Task<Member?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Id == id));
        public Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Email == email));
        public Task<Member?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Uuid == uuid));
        public Task<PagedResult<Member>> GetAllAsync(string? status, string? search, string? labelId, int page, int pageSize, MemberEngagementFilter? engagement = null, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Member>> GetAllForExportAsync(string? status, string? labelId, MemberEngagementFilter? engagement = null, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Member>> GetByLabelAsync(string labelId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Member>> GetNewsletterSubscribersAsync(string newsletterId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task AddAsync(Member member, CancellationToken ct = default)
        { Members.Add(member); return Task.CompletedTask; }
        public Task UpdateAsync(Member member, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Members.RemoveAll(m => m.Id == id); return Task.CompletedTask; }
        public Task<int> GetCountAsync(string? status, CancellationToken ct = default)
            => Task.FromResult(Members.Count);
        public Task AddLabelToMemberAsync(string memberId, string labelId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task RemoveLabelFromMemberAsync(string memberId, string labelId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task UpdateNoteAsync(string memberId, string? note, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<bool> LinkStripeCustomerAsync(string memberId, string stripeCustomerId, string? name, string? email, CancellationToken ct = default)
            => Task.FromResult(true);
        public Task AddCreatedEventAsync(MembersCreatedEvent evt, CancellationToken ct = default)
        { CreatedEvents.Add(evt); return Task.CompletedTask; }
        public Task AddStatusEventAsync(MembersStatusEvent evt, CancellationToken ct = default)
        { StatusEvents.Add(evt); return Task.CompletedTask; }
        public Task AddSubscribeEventAsync(MembersSubscribeEvent evt, CancellationToken ct = default)
        { SubscribeEvents.Add(evt); return Task.CompletedTask; }
    }

    private sealed class FakeNewsletterRepo : INewsletterRepository
    {
        public List<Newsletter> Active { get; } = [];

        public Task<Newsletter?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Active.FirstOrDefault(n => n.Id == id));
        public Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Active.FirstOrDefault(n => n.Slug == slug));
        public Task<List<Newsletter>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Active.ToList());
        public Task<List<Newsletter>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult(Active.Where(n => n.Status == "active").ToList());
        public Task AddAsync(Newsletter newsletter, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task UpdateAsync(Newsletter newsletter, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<int> GetSubscriberCountAsync(string newsletterId, CancellationToken ct = default)
            => Task.FromResult(0);
        public Task<NewsletterAnalytics> GetAnalyticsAsync(string newsletterId, int sendsLimit = 10, CancellationToken ct = default)
            => Task.FromResult(new NewsletterAnalytics(0, 0, 0, 0, 0, 0, []));
        public Task<IReadOnlyList<NewsletterGrowthPoint>> GetGrowthAsync(string newsletterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsletterGrowthPoint>>([]);
    }

    private sealed class FakeNewsletterService : INewsletterService
    {
        public List<(string MemberId, string NewsletterId)> Subscriptions { get; } = [];

        public Task<Email> SendPostAsNewsletterAsync(SendNewsletterRequest request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task ProcessPendingEmailAsync(string emailId, string siteUrl, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<Email> SendAbTestWinnerAsync(string emailId, string siteUrl, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task RecordOpenAsync(string emailRecipientId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<string> RecordClickAsync(string redirectId, string memberId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task SubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default)
        { Subscriptions.Add((memberId, newsletterId)); return Task.CompletedTask; }
        public Task UnsubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<List<string>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default)
            => Task.FromResult(new List<string>());
        public Task RecordFeedbackAsync(string emailId, string memberId, int score, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAutomatedEmailService : IAutomatedEmailService
    {
        public List<(string Slug, string MemberEmail)> SentEmails { get; } = [];

        public Task SendAsync(string slug, Member member, string siteUrl, CancellationToken ct = default)
        {
            SentEmails.Add((slug, member.Email));
            return Task.CompletedTask;
        }

        public Task DispatchScheduledAsync(string scheduleId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<AutomatedEmail?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult<AutomatedEmail?>(null);
        public Task<AutomatedEmail?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<AutomatedEmail?>(null);
        public Task<List<AutomatedEmail>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<AutomatedEmail>());
        public Task<AutomatedEmail> CreateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
            => Task.FromResult(automatedEmail);
        public Task UpdateAsync(AutomatedEmail automatedEmail, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<int> GetRecipientCountAsync(string automatedEmailId, CancellationToken ct = default)
            => Task.FromResult(0);

        public Task<AutomatedEmailStatistics> GetStatisticsAsync(string automatedEmailId, CancellationToken ct = default)
            => Task.FromResult(AutomatedEmailStatistics.Empty);

        public Task<Dictionary<string, AutomatedEmailStatistics>> GetStatisticsBatchAsync(
            IEnumerable<string> automatedEmailIds, CancellationToken ct = default)
            => Task.FromResult(new Dictionary<string, AutomatedEmailStatistics>());

        public Task<PagedResult<AutomatedEmailRecipient>> GetRecipientsAsync(
            string automatedEmailId, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<AutomatedEmailRecipient>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
            });

        public Task<string?> RecordDeliveryEventAsync(
            string email, string eventType, DateTime occurredAt, string? failureReason, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
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

    private sealed class FakeWebhookDispatch : IWebhookDispatchService
    {
        public List<(string Event, object Payload)> Enqueued { get; } = [];
        public void Enqueue(string eventName, object payload) => Enqueued.Add((eventName, payload));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
