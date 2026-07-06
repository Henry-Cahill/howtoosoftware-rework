using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Core.Tests;

public class SuppressionServiceTests
{
    private readonly FakeMemberRepo _memberRepo = new();
    private readonly FakeEmailRepo _emailRepo = new();
    private readonly SuppressionService _sut;

    public SuppressionServiceTests()
    {
        _sut = new SuppressionService(
            _emailRepo, _memberRepo,
            NullLogger<SuppressionService>.Instance);
    }

    // ── HandleBounceAsync ───────────────────────────────────────

    [Fact]
    public async Task HandleBounce_CreatesSuppression_AndDisablesMember()
    {
        var member = AddMember("user@example.com");

        await _sut.HandleBounceAsync("user@example.com", "email-001");

        Assert.Single(_emailRepo.Suppressions);
        Assert.Equal("bounce", _emailRepo.Suppressions[0].Reason);
        Assert.Equal("user@example.com", _emailRepo.Suppressions[0].Email);
        Assert.Equal("email-001", _emailRepo.Suppressions[0].EmailId);
        Assert.True(member.EmailDisabled);
    }

    [Fact]
    public async Task HandleBounce_SkipsDuplicateSuppression()
    {
        AddMember("user@example.com");
        _emailRepo.SuppressedEmails.Add("user@example.com");

        await _sut.HandleBounceAsync("user@example.com", "email-001");

        // Should NOT add a second suppression record
        Assert.Empty(_emailRepo.Suppressions);
    }

    [Fact]
    public async Task HandleBounce_StillDisablesMember_WhenAlreadySuppressed()
    {
        var member = AddMember("user@example.com");
        _emailRepo.SuppressedEmails.Add("user@example.com");

        await _sut.HandleBounceAsync("user@example.com", "email-001");

        Assert.True(member.EmailDisabled);
    }

    [Fact]
    public async Task HandleBounce_NoMember_StillCreatesSuppression()
    {
        // Bounce for an email not in members table (e.g. import glitch)
        await _sut.HandleBounceAsync("unknown@example.com", "email-001");

        Assert.Single(_emailRepo.Suppressions);
        Assert.Equal("bounce", _emailRepo.Suppressions[0].Reason);
    }

    [Fact]
    public async Task HandleBounce_NullEmailId_Accepted()
    {
        AddMember("user@example.com");

        await _sut.HandleBounceAsync("user@example.com", null);

        Assert.Single(_emailRepo.Suppressions);
        Assert.Null(_emailRepo.Suppressions[0].EmailId);
        Assert.True(_memberRepo.Members[0].EmailDisabled);
    }

    // ── HandleSpamComplaintAsync ─────────────────────────────────

    [Fact]
    public async Task HandleSpamComplaint_CreatesSuppression_AndEvent_AndDisablesMember()
    {
        var member = AddMember("user@example.com");

        await _sut.HandleSpamComplaintAsync("user@example.com", "email-002");

        Assert.Single(_emailRepo.Suppressions);
        Assert.Equal("spam", _emailRepo.Suppressions[0].Reason);

        Assert.Single(_emailRepo.SpamComplaintEvents);
        Assert.Equal(member.Id, _emailRepo.SpamComplaintEvents[0].MemberId);
        Assert.Equal("email-002", _emailRepo.SpamComplaintEvents[0].EmailId);
        Assert.Equal("user@example.com", _emailRepo.SpamComplaintEvents[0].EmailAddress);

        Assert.True(member.EmailDisabled);
    }

    [Fact]
    public async Task HandleSpamComplaint_NoMember_SkipsEventButSuppresses()
    {
        await _sut.HandleSpamComplaintAsync("unknown@example.com", "email-002");

        Assert.Single(_emailRepo.Suppressions);
        Assert.Empty(_emailRepo.SpamComplaintEvents);
    }

    [Fact]
    public async Task HandleSpamComplaint_NullEmailId_SkipsEvent()
    {
        AddMember("user@example.com");

        await _sut.HandleSpamComplaintAsync("user@example.com", null);

        Assert.Single(_emailRepo.Suppressions);
        Assert.Empty(_emailRepo.SpamComplaintEvents); // Can't record without emailId
        Assert.True(_memberRepo.Members[0].EmailDisabled);
    }

    // ── RemoveSuppressionAsync ──────────────────────────────────

    [Fact]
    public async Task RemoveSuppression_RemovesRecord_AndReenablesMember()
    {
        var member = AddMember("user@example.com");
        member.EmailDisabled = true;
        _emailRepo.SuppressedEmails.Add("user@example.com");

        await _sut.RemoveSuppressionAsync("user@example.com");

        Assert.DoesNotContain("user@example.com", _emailRepo.RemovedSuppressions);
        // Verify removal was called
        Assert.Contains("user@example.com", _emailRepo.RemoveRequests);
        Assert.False(member.EmailDisabled);
    }

    [Fact]
    public async Task RemoveSuppression_NoMember_DoesNotThrow()
    {
        await _sut.RemoveSuppressionAsync("gone@example.com");

        Assert.Contains("gone@example.com", _emailRepo.RemoveRequests);
    }

    [Fact]
    public async Task HandleBounce_MemberAlreadyDisabled_StaysDisabled()
    {
        var member = AddMember("user@example.com");
        member.EmailDisabled = true;

        await _sut.HandleBounceAsync("user@example.com", "email-003");

        Assert.True(member.EmailDisabled);
        Assert.Single(_emailRepo.Suppressions);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private Member AddMember(string email)
    {
        var member = new Member
        {
            Id = Guid.NewGuid().ToString("D"),
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = email,
            Status = "free",
            CreatedAt = DateTime.UtcNow,
        };
        _memberRepo.Members.Add(member);
        return member;
    }

    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakeMemberRepo : IMemberRepository
    {
        public List<Member> Members { get; } = [];

        public Task<Member?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Id == id));
        public Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
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
        public Task<bool> LinkStripeCustomerAsync(string memberId, string stripeCustomerId, string? name, string? email, CancellationToken ct = default)
            => Task.FromResult(true);
        public Task UpdateNoteAsync(string memberId, string? note, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddCreatedEventAsync(MembersCreatedEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddStatusEventAsync(MembersStatusEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddSubscribeEventAsync(MembersSubscribeEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailRepo : IEmailRepository
    {
        public List<Suppression> Suppressions { get; } = [];
        public HashSet<string> SuppressedEmails { get; } = [];
        public List<string> RemoveRequests { get; } = [];
        public HashSet<string> RemovedSuppressions { get; } = [];
        public List<EmailSpamComplaintEvent> SpamComplaintEvents { get; } = [];

        public Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
            => Task.FromResult<ITransactionScope>(new NoOpTransactionScope());

        public Task<bool> IsEmailSuppressedAsync(string email, CancellationToken ct = default)
            => Task.FromResult(SuppressedEmails.Contains(email));
        public Task AddSuppressionAsync(Suppression suppression, CancellationToken ct = default)
        { Suppressions.Add(suppression); SuppressedEmails.Add(suppression.Email); return Task.CompletedTask; }
        public Task RemoveSuppressionAsync(string email, CancellationToken ct = default)
        { RemoveRequests.Add(email); SuppressedEmails.Remove(email); return Task.CompletedTask; }
        public Task AddSpamComplaintEventAsync(EmailSpamComplaintEvent evt, CancellationToken ct = default)
        { SpamComplaintEvents.Add(evt); return Task.CompletedTask; }

        // Unused by SuppressionService — stubs only
        public Task<List<string>> GetSuppressedEmailsAsync(CancellationToken ct = default)
            => Task.FromResult(SuppressedEmails.ToList());
        public Task<Email?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Email?>(null);
        public Task<List<Email>> GetPendingEmailsAsync(CancellationToken ct = default) => Task.FromResult<List<Email>>([]);
        public Task<List<Email>> GetAbTestsAwaitingWinnerAsync(DateTime now, CancellationToken ct = default) => Task.FromResult<List<Email>>([]);
        public Task<List<EmailRecipient>> GetHoldoutRecipientsAsync(string emailId, CancellationToken ct = default) => Task.FromResult<List<EmailRecipient>>([]);
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
        public Task<MembersNewsletter?> GetSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default) => Task.FromResult<MembersNewsletter?>(null);
        public Task<List<MembersNewsletter>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default) => Task.FromResult<List<MembersNewsletter>>([]);
        public Task AddSubscriptionAsync(MembersNewsletter subscription, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<MembersFeedback?> GetFeedbackAsync(string memberId, string postId, CancellationToken ct = default) => Task.FromResult<MembersFeedback?>(null);
        public Task AddFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default) => Task.CompletedTask;
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
