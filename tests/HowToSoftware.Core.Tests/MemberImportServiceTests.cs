using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Core.Tests;

public class MemberImportServiceTests
{
    private readonly FakeMemberRepo _members = new();
    private readonly FakeLabelRepo _labels = new();
    private readonly FakeNewsletterRepo _newsletters = new();
    private readonly FakeNewsletterService _newsletterService = new();
    private readonly MemberImportService _sut;

    public MemberImportServiceTests()
    {
        _sut = new MemberImportService(
            _members, _labels, _newsletters, _newsletterService,
            NullLogger<MemberImportService>.Instance);
    }

    [Fact]
    public async Task ImportAsync_EmptyCsv_ReturnsError()
    {
        var result = await _sut.ImportAsync("");
        Assert.Equal(0, result.Imported);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ImportAsync_MissingEmailHeader_ReturnsError()
    {
        var csv = "name,status\nAlice,free\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(0, result.Imported);
        Assert.Contains(result.Errors, e => e.Message.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportAsync_ValidRow_ImportsMember()
    {
        var csv = "email,name,status\nalice@example.com,Alice,free\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);
        var m = Assert.Single(_members.Members);
        Assert.Equal("alice@example.com", m.Email);
        Assert.Equal("Alice", m.Name);
        Assert.Equal("free", m.Status);
        Assert.False(m.EmailDisabled);
    }

    [Fact]
    public async Task ImportAsync_InvalidEmail_FailsRow()
    {
        var csv = "email,name\nnot-an-email,Bob\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Empty(_members.Members);
    }

    [Fact]
    public async Task ImportAsync_DuplicateInDb_Skipped()
    {
        _members.Members.Add(new Member { Id = "existing", Email = "alice@example.com", Status = "free" });
        var csv = "email\nalice@example.com\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.SkippedDuplicates);
        Assert.Single(_members.Members);
    }

    [Fact]
    public async Task ImportAsync_DuplicateInBatch_Skipped()
    {
        var csv = "email\nalice@example.com\nalice@example.com\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.SkippedDuplicates);
    }

    [Fact]
    public async Task ImportAsync_ComplimentaryPlanTrue_SetsComped()
    {
        var csv = "email,complimentary_plan\nalice@example.com,true\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.Equal("comped", _members.Members[0].Status);
    }

    [Fact]
    public async Task ImportAsync_SubscribedFalse_DisablesEmail()
    {
        var csv = "email,subscribed_to_emails\nalice@example.com,false\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.True(_members.Members[0].EmailDisabled);
        Assert.Empty(_newsletterService.Subscriptions);
    }

    [Fact]
    public async Task ImportAsync_SubscribedTrue_SubscribesToDefaultNewsletters()
    {
        _newsletters.Active.Add(new Newsletter { Id = "nl1", Slug = "nl1", Name = "NL1", Status = "active", SubscribeOnSignup = true });
        _newsletters.Active.Add(new Newsletter { Id = "nl2", Slug = "nl2", Name = "NL2", Status = "active", SubscribeOnSignup = false });

        var csv = "email,subscribed_to_emails\nalice@example.com,true\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.Single(_newsletterService.Subscriptions);
        Assert.Equal("nl1", _newsletterService.Subscriptions[0].NewsletterId);
    }

    [Fact]
    public async Task ImportAsync_LabelsColumn_CreatesAndAssigns()
    {
        _labels.Items.Add(new Label { Id = "vip", Name = "VIP", Slug = "vip" });
        var csv = "email,labels\nalice@example.com,VIP;Beta Tester\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.LabelsCreated);
        Assert.Equal(2, _members.LabelAssignments.Count);
        // Existing label "VIP" reused; new "Beta Tester" created.
        Assert.Contains(_labels.Items, l => l.Name == "Beta Tester");
    }

    [Fact]
    public async Task ImportAsync_StripeCustomerId_LinksOnce()
    {
        var csv = "email,stripe_customer_id\nalice@example.com,cus_123\nbob@example.com,cus_123\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.StripeCustomersLinked);
        Assert.Contains(result.Errors, e => e.Message.Contains("already linked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportAsync_QuotedFieldsAndCommas_Parsed()
    {
        var csv = "email,name\n\"alice@example.com\",\"Doe, Alice\"\n";
        var result = await _sut.ImportAsync(csv);
        Assert.Equal(1, result.Imported);
        Assert.Equal("Doe, Alice", _members.Members[0].Name);
    }

    [Fact]
    public async Task ImportAsync_RecordsCreatedAndStatusEvents()
    {
        var csv = "email\nalice@example.com\n";
        await _sut.ImportAsync(csv);
        Assert.Single(_members.CreatedEvents);
        Assert.Equal("import", _members.CreatedEvents[0].Source);
        Assert.Single(_members.StatusEvents);
        Assert.Equal("free", _members.StatusEvents[0].ToStatus);
    }

    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakeMemberRepo : IMemberRepository
    {
        public List<Member> Members { get; } = [];
        public List<MembersCreatedEvent> CreatedEvents { get; } = [];
        public List<MembersStatusEvent> StatusEvents { get; } = [];
        public List<(string MemberId, string LabelId)> LabelAssignments { get; } = [];
        public HashSet<string> LinkedStripeCustomers { get; } = new(StringComparer.Ordinal);

        public Task<Member?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Id == id));
        public Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<Member?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult<Member?>(null);
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
        public Task UpdateAsync(Member member, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> GetCountAsync(string? status, CancellationToken ct = default) => Task.FromResult(Members.Count);
        public Task AddLabelToMemberAsync(string memberId, string labelId, CancellationToken ct = default)
        { LabelAssignments.Add((memberId, labelId)); return Task.CompletedTask; }
        public Task RemoveLabelFromMemberAsync(string memberId, string labelId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task UpdateNoteAsync(string memberId, string? note, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<bool> LinkStripeCustomerAsync(string memberId, string stripeCustomerId, string? name, string? email, CancellationToken ct = default)
            => Task.FromResult(LinkedStripeCustomers.Add(stripeCustomerId));
        public Task AddCreatedEventAsync(MembersCreatedEvent evt, CancellationToken ct = default)
        { CreatedEvents.Add(evt); return Task.CompletedTask; }
        public Task AddStatusEventAsync(MembersStatusEvent evt, CancellationToken ct = default)
        { StatusEvents.Add(evt); return Task.CompletedTask; }
        public Task AddSubscribeEventAsync(MembersSubscribeEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeLabelRepo : ILabelRepository
    {
        public List<Label> Items { get; } = [];

        public Task<List<Label>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Items.ToList());
        public Task<Label?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(l => l.Id == id));
        public Task<Label?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(l => l.Slug == slug));
        public Task AddAsync(Label label, CancellationToken ct = default)
        { Items.Add(label); return Task.CompletedTask; }
        public Task UpdateAsync(Label label, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeNewsletterRepo : INewsletterRepository
    {
        public List<Newsletter> Active { get; } = [];
        public Task<Newsletter?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Active.FirstOrDefault(n => n.Id == id));
        public Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Active.FirstOrDefault(n => n.Slug == slug));
        public Task<List<Newsletter>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Active.ToList());
        public Task<List<Newsletter>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult(Active.Where(n => n.Status == "active").ToList());
        public Task AddAsync(Newsletter newsletter, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Newsletter newsletter, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> GetSubscriberCountAsync(string newsletterId, CancellationToken ct = default) => Task.FromResult(0);
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
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
