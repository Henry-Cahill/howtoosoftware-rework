using HowToSoftware.Core.Entities;
using HowToSoftware.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class EmailRepositoryTests(DatabaseFixture db) : IAsyncLifetime
{
    private string _postId = null!;
    private string _memberId = null!;
    private string _newsletterId = null!;

    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();

        // Clean dependent tables first
        await ctx.EmailRecipients.ExecuteDeleteAsync();
        await ctx.EmailBatches.ExecuteDeleteAsync();
        await ctx.MembersFeedback.ExecuteDeleteAsync();
        await ctx.MembersClickEvents.ExecuteDeleteAsync();
        await ctx.Redirects.ExecuteDeleteAsync();
        await ctx.Suppressions.ExecuteDeleteAsync();
        await ctx.EmailSpamComplaintEvents.ExecuteDeleteAsync();
        await ctx.MembersNewsletters.ExecuteDeleteAsync();
        await ctx.Emails.ExecuteDeleteAsync();
        await ctx.Newsletters.ExecuteDeleteAsync();
        await ctx.Members.ExecuteDeleteAsync();
        await ctx.Posts.ExecuteDeleteAsync();

        // Seed shared data
        _postId = Guid.NewGuid().ToString("N")[..24];
        _memberId = Guid.NewGuid().ToString("N")[..24];
        _newsletterId = Guid.NewGuid().ToString("N")[..24];

        ctx.Posts.Add(new Post
        {
            Id = _postId,
            Uuid = Guid.NewGuid().ToString("D"),
            Title = "Test Post",
            Slug = "test-post",
            Status = "published",
            CreatedAt = DateTime.UtcNow,
        });
        ctx.Members.Add(new Member
        {
            Id = _memberId,
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = "member@test.com",
            CreatedAt = DateTime.UtcNow,
        });
        ctx.Newsletters.Add(new Newsletter
        {
            Id = _newsletterId,
            Uuid = Guid.NewGuid().ToString("D"),
            Name = "Default",
            Slug = "default",
            CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private EmailRepository CreateRepo() => new(db.CreateContext());

    private Email MakeEmail(string status = "pending") => new()
    {
        Id = Guid.NewGuid().ToString("N")[..24],
        PostId = _postId,
        Uuid = Guid.NewGuid().ToString("D"),
        Status = status,
        Subject = "Test Email",
        Html = "<p>Hello</p>",
        NewsletterId = _newsletterId,
        SubmittedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
    };

    // ── Email CRUD ─────────────────────────────────────────────

    [Fact]
    public async Task AddEmailAsync_And_GetByIdAsync_RoundTrips()
    {
        var repo = CreateRepo();
        var email = MakeEmail();
        await repo.AddEmailAsync(email);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByIdAsync(email.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Test Email", loaded.Subject);
        Assert.Equal("pending", loaded.Status);
    }

    [Fact]
    public async Task GetPendingEmailsAsync_OnlyReturnsPending()
    {
        var repo = CreateRepo();
        await repo.AddEmailAsync(MakeEmail("pending"));
        await repo.AddEmailAsync(MakeEmail("submitted"));
        await repo.AddEmailAsync(MakeEmail("pending"));

        var repo2 = CreateRepo();
        var pending = await repo2.GetPendingEmailsAsync();

        Assert.Equal(2, pending.Count);
        Assert.All(pending, e => Assert.Equal("pending", e.Status));
    }

    [Fact]
    public async Task UpdateEmailAsync_PersistsChanges()
    {
        var repo = CreateRepo();
        var email = MakeEmail();
        await repo.AddEmailAsync(email);

        await using var ctx2 = db.CreateContext();
        var toUpdate = await ctx2.Emails.FirstAsync(e => e.Id == email.Id);
        toUpdate.Status = "submitted";
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var repo2 = new EmailRepository(ctx2);
        await repo2.UpdateEmailAsync(toUpdate);

        var repo3 = CreateRepo();
        var loaded = await repo3.GetByIdAsync(email.Id);
        Assert.Equal("submitted", loaded!.Status);
    }

    // ── Batches ────────────────────────────────────────────────

    [Fact]
    public async Task AddBatchAsync_And_UpdateBatchAsync_Work()
    {
        var repo = CreateRepo();
        var email = MakeEmail();
        await repo.AddEmailAsync(email);

        var batch = new EmailBatch
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            EmailId = email.Id,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await repo.AddBatchAsync(batch);

        await using var ctx2 = db.CreateContext();
        var toUpdate = await ctx2.EmailBatches.FirstAsync(b => b.Id == batch.Id);
        toUpdate.Status = "submitted";
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var repo2 = new EmailRepository(ctx2);
        await repo2.UpdateBatchAsync(toUpdate);

        await using var ctx3 = db.CreateContext();
        var loaded = await ctx3.EmailBatches.FirstAsync(b => b.Id == batch.Id);
        Assert.Equal("submitted", loaded.Status);
    }

    // ── Recipients ─────────────────────────────────────────────

    [Fact]
    public async Task AddRecipientsAsync_And_GetRecipientByIdAsync_Work()
    {
        var repo = CreateRepo();
        var email = MakeEmail();
        await repo.AddEmailAsync(email);

        var batch = new EmailBatch
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            EmailId = email.Id,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await repo.AddBatchAsync(batch);

        var recipientId = Guid.NewGuid().ToString("N")[..24];
        await repo.AddRecipientsAsync([
            new EmailRecipient
            {
                Id = recipientId,
                EmailId = email.Id,
                BatchId = batch.Id,
                MemberId = _memberId,
                MemberUuid = Guid.NewGuid().ToString("D"),
                MemberEmail = "member@test.com",
                MemberName = "Test Member",
            }
        ]);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetRecipientByIdAsync(recipientId);

        Assert.NotNull(loaded);
        Assert.Equal("member@test.com", loaded.MemberEmail);
    }

    // ── Suppressions ───────────────────────────────────────────

    [Fact]
    public async Task AddSuppressionAsync_And_IsEmailSuppressedAsync()
    {
        var repo = CreateRepo();
        await repo.AddSuppressionAsync(new Suppression
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Email = "bounced@test.com",
            Reason = "hard_bounce",
            CreatedAt = DateTime.UtcNow,
        });

        var repo2 = CreateRepo();
        Assert.True(await repo2.IsEmailSuppressedAsync("bounced@test.com"));
        Assert.False(await repo2.IsEmailSuppressedAsync("good@test.com"));
    }

    [Fact]
    public async Task GetSuppressedEmailsAsync_ReturnsDistinctEmails()
    {
        var repo = CreateRepo();
        await repo.AddSuppressionAsync(new Suppression
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Email = "sup1@test.com",
            Reason = "bounce",
            CreatedAt = DateTime.UtcNow,
        });
        await repo.AddSuppressionAsync(new Suppression
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Email = "sup2@test.com",
            Reason = "spam",
            CreatedAt = DateTime.UtcNow,
        });

        var repo2 = CreateRepo();
        var suppressed = await repo2.GetSuppressedEmailsAsync();

        Assert.Contains("sup1@test.com", suppressed);
        Assert.Contains("sup2@test.com", suppressed);
    }

    [Fact]
    public async Task RemoveSuppressionAsync_RemovesSuppression()
    {
        var repo = CreateRepo();
        await repo.AddSuppressionAsync(new Suppression
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Email = "remove-sup@test.com",
            Reason = "bounce",
            CreatedAt = DateTime.UtcNow,
        });

        var repo2 = CreateRepo();
        await repo2.RemoveSuppressionAsync("remove-sup@test.com");

        var repo3 = CreateRepo();
        Assert.False(await repo3.IsEmailSuppressedAsync("remove-sup@test.com"));
    }

    // ── Subscriptions ──────────────────────────────────────────

    [Fact]
    public async Task AddSubscriptionAsync_And_GetSubscriptionAsync()
    {
        var repo = CreateRepo();
        await repo.AddSubscriptionAsync(new MembersNewsletter
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = _memberId,
            NewsletterId = _newsletterId,
        });

        var repo2 = CreateRepo();
        var sub = await repo2.GetSubscriptionAsync(_memberId, _newsletterId);

        Assert.NotNull(sub);
        Assert.Equal(_memberId, sub.MemberId);
    }

    [Fact]
    public async Task GetMemberSubscriptionsAsync_ReturnsAll()
    {
        var repo = CreateRepo();
        await repo.AddSubscriptionAsync(new MembersNewsletter
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = _memberId,
            NewsletterId = _newsletterId,
        });

        var repo2 = CreateRepo();
        var subs = await repo2.GetMemberSubscriptionsAsync(_memberId);

        Assert.Single(subs);
    }

    [Fact]
    public async Task RemoveSubscriptionAsync_RemovesSubscription()
    {
        var repo = CreateRepo();
        await repo.AddSubscriptionAsync(new MembersNewsletter
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = _memberId,
            NewsletterId = _newsletterId,
        });

        var repo2 = CreateRepo();
        await repo2.RemoveSubscriptionAsync(_memberId, _newsletterId);

        var repo3 = CreateRepo();
        var sub = await repo3.GetSubscriptionAsync(_memberId, _newsletterId);
        Assert.Null(sub);
    }

    // ── Feedback ───────────────────────────────────────────────

    [Fact]
    public async Task AddFeedbackAsync_And_GetFeedbackAsync()
    {
        var repo = CreateRepo();
        await repo.AddFeedbackAsync(new MembersFeedback
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            MemberId = _memberId,
            PostId = _postId,
            Score = 1,
            CreatedAt = DateTime.UtcNow,
        });

        var repo2 = CreateRepo();
        var fb = await repo2.GetFeedbackAsync(_memberId, _postId);

        Assert.NotNull(fb);
        Assert.Equal(1, fb.Score);
    }

    [Fact]
    public async Task GetFeedbackAsync_NotFound_ReturnsNull()
    {
        var repo = CreateRepo();
        var fb = await repo.GetFeedbackAsync("nonexistent0000000000000", _postId);

        Assert.Null(fb);
    }

    // ── Redirects & Click events ───────────────────────────────

    [Fact]
    public async Task AddRedirectAsync_And_GetRedirectByIdAsync()
    {
        var repo = CreateRepo();
        var redirectId = Guid.NewGuid().ToString("N")[..24];
        await repo.AddRedirectAsync(new Redirect
        {
            Id = redirectId,
            From = "/old-path",
            To = "/new-path",
            PostId = _postId,
            CreatedAt = DateTime.UtcNow,
        });

        var repo2 = CreateRepo();
        var loaded = await repo2.GetRedirectByIdAsync(redirectId);

        Assert.NotNull(loaded);
        Assert.Equal("/old-path", loaded.From);
        Assert.Equal("/new-path", loaded.To);
    }

    [Fact]
    public async Task AddClickEventAsync_PersistsEvent()
    {
        await using var ctx = db.CreateContext();
        var redirect = new Redirect
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            From = "/click-from",
            To = "/click-to",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Redirects.Add(redirect);
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        await repo.AddClickEventAsync(new MembersClickEvent
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            MemberId = _memberId,
            RedirectId = redirect.Id,
            CreatedAt = DateTime.UtcNow,
        });

        await using var ctx2 = db.CreateContext();
        var clicks = await ctx2.MembersClickEvents
            .Where(c => c.MemberId == _memberId)
            .ToListAsync();
        Assert.Single(clicks);
    }

    // ── Spam complaint ─────────────────────────────────────────

    [Fact]
    public async Task AddSpamComplaintEventAsync_PersistsEvent()
    {
        var repo = CreateRepo();
        var email = MakeEmail();
        await repo.AddEmailAsync(email);

        var repo2 = CreateRepo();
        await repo2.AddSpamComplaintEventAsync(new EmailSpamComplaintEvent
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            MemberId = _memberId,
            EmailId = email.Id,
            EmailAddress = "member@test.com",
            CreatedAt = DateTime.UtcNow,
        });

        await using var ctx = db.CreateContext();
        var complaints = await ctx.EmailSpamComplaintEvents.ToListAsync();
        Assert.Single(complaints);
        Assert.Equal("member@test.com", complaints[0].EmailAddress);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
