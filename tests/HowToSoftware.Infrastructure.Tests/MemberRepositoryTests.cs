using HowToSoftware.Core.Entities;
using HowToSoftware.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class MemberRepositoryTests(DatabaseFixture db) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        await ctx.Members.ExecuteDeleteAsync();
        await ctx.Labels.ExecuteDeleteAsync();
        await ctx.Newsletters.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private MemberRepository CreateRepo() => new(db.CreateContext());

    private static Member MakeMember(string email, string name = "Test User", string status = "free")
    {
        var id = Guid.NewGuid().ToString("N")[..24];
        return new Member
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = email,
            Name = name,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Add / GetById ──────────────────────────────────────────

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_RoundTrips()
    {
        var repo = CreateRepo();
        var member = MakeMember("roundtrip@test.com");
        await repo.AddAsync(member);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByIdAsync(member.Id);

        Assert.NotNull(loaded);
        Assert.Equal("roundtrip@test.com", loaded.Email);
    }

    // ── GetByEmail ─────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_FindsMember()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeMember("findme@test.com"));

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByEmailAsync("findme@test.com");

        Assert.NotNull(loaded);
        Assert.Equal("findme@test.com", loaded.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_NotFound_ReturnsNull()
    {
        var repo = CreateRepo();
        var result = await repo.GetByEmailAsync("nope@test.com");

        Assert.Null(result);
    }

    // ── GetByUuid ──────────────────────────────────────────────

    [Fact]
    public async Task GetByUuidAsync_FindsMember()
    {
        var repo = CreateRepo();
        var member = MakeMember("uuid@test.com");
        await repo.AddAsync(member);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByUuidAsync(member.Uuid);

        Assert.NotNull(loaded);
        Assert.Equal(member.Id, loaded.Id);
    }

    // ── GetAllAsync (paging, filtering, search) ────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllMembers()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeMember("a@test.com"));
        await repo.AddAsync(MakeMember("b@test.com"));

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeMember("free@test.com", status: "free"));
        await repo.AddAsync(MakeMember("paid@test.com", status: "paid"));

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync("paid", null, null, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("paid@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_SearchByEmail()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeMember("alice@example.com", "Alice"));
        await repo.AddAsync(MakeMember("bob@other.com", "Bob"));

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, "alice", null, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("alice@example.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_SearchByName()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeMember("a@test.com", "Alice Smith"));
        await repo.AddAsync(MakeMember("b@test.com", "Bob Jones"));

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, "Alice", null, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Alice Smith", result.Items[0].Name);
    }

    // ── Engagement filter (MEM.4) ──────────────────────────────

    [Fact]
    public async Task GetAllAsync_EngagementActiveLast30Days_OnlyRecentlySeen()
    {
        var repo = CreateRepo();
        var now = DateTime.UtcNow;

        var recent = MakeMember("recent@test.com");
        recent.LastSeenAt = now.AddDays(-5);
        await repo.AddAsync(recent);

        var stale = MakeMember("stale@test.com");
        stale.LastSeenAt = now.AddDays(-60);
        await repo.AddAsync(stale);

        var never = MakeMember("never@test.com");
        never.LastSeenAt = null;
        await repo.AddAsync(never);

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, null, null, 1, 10,
            HowToSoftware.Core.Interfaces.MemberEngagementFilter.ActiveLast30Days);

        Assert.Single(result.Items);
        Assert.Equal("recent@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_EngagementNeverOpenedEmail_RequiresSentAndZeroOpens()
    {
        var repo = CreateRepo();

        var sentNotOpened = MakeMember("sent-not-opened@test.com");
        sentNotOpened.EmailCount = 5;
        sentNotOpened.EmailOpenedCount = 0;
        await repo.AddAsync(sentNotOpened);

        var openedSome = MakeMember("opened-some@test.com");
        openedSome.EmailCount = 5;
        openedSome.EmailOpenedCount = 1;
        await repo.AddAsync(openedSome);

        // Never sent any email — should NOT match "never opened".
        var noEmailsSent = MakeMember("no-emails@test.com");
        noEmailsSent.EmailCount = 0;
        noEmailsSent.EmailOpenedCount = 0;
        await repo.AddAsync(noEmailsSent);

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, null, null, 1, 10,
            HowToSoftware.Core.Interfaces.MemberEngagementFilter.NeverOpenedEmail);

        Assert.Single(result.Items);
        Assert.Equal("sent-not-opened@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_EngagementOpenedOverHalf_StrictlyOverFiftyPercent()
    {
        var repo = CreateRepo();

        var highEngagement = MakeMember("high@test.com");
        highEngagement.EmailCount = 10;
        highEngagement.EmailOpenedCount = 6; // 60%
        await repo.AddAsync(highEngagement);

        var exactlyHalf = MakeMember("half@test.com");
        exactlyHalf.EmailCount = 10;
        exactlyHalf.EmailOpenedCount = 5; // 50% — excluded
        await repo.AddAsync(exactlyHalf);

        var lowEngagement = MakeMember("low@test.com");
        lowEngagement.EmailCount = 10;
        lowEngagement.EmailOpenedCount = 2; // 20%
        await repo.AddAsync(lowEngagement);

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, null, null, 1, 10,
            HowToSoftware.Core.Interfaces.MemberEngagementFilter.OpenedOverHalf);

        Assert.Single(result.Items);
        Assert.Equal("high@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_EngagementInactiveLast90Days_IncludesNullAndOld()
    {
        var repo = CreateRepo();
        var now = DateTime.UtcNow;

        var recent = MakeMember("recent@test.com");
        recent.LastSeenAt = now.AddDays(-10);
        await repo.AddAsync(recent);

        var stale = MakeMember("stale@test.com");
        stale.LastSeenAt = now.AddDays(-120);
        await repo.AddAsync(stale);

        var never = MakeMember("never@test.com");
        never.LastSeenAt = null;
        await repo.AddAsync(never);

        var repo2 = CreateRepo();
        var result = await repo2.GetAllAsync(null, null, null, 1, 10,
            HowToSoftware.Core.Interfaces.MemberEngagementFilter.InactiveLast90Days);

        var emails = result.Items.Select(m => m.Email).OrderBy(e => e).ToList();
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "never@test.com", "stale@test.com" }, emails);
    }

    // ── Delete ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesMember()
    {
        var repo = CreateRepo();
        var member = MakeMember("delete@test.com");
        await repo.AddAsync(member);

        var repo2 = CreateRepo();
        await repo2.DeleteAsync(member.Id);

        var repo3 = CreateRepo();
        Assert.Null(await repo3.GetByIdAsync(member.Id));
    }

    // ── Count ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCountAsync_FiltersCorrectly()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeMember("c1@test.com", status: "free"));
        await repo.AddAsync(MakeMember("c2@test.com", status: "free"));
        await repo.AddAsync(MakeMember("c3@test.com", status: "paid"));

        var repo2 = CreateRepo();
        Assert.Equal(3, await repo2.GetCountAsync(null));
        Assert.Equal(2, await repo2.GetCountAsync("free"));
        Assert.Equal(1, await repo2.GetCountAsync("paid"));
    }

    // ── Labels ─────────────────────────────────────────────────

    [Fact]
    public async Task AddLabelToMemberAsync_AssociatesLabel()
    {
        await using var ctx = db.CreateContext();
        var label = new Label
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Name = "VIP",
            Slug = "vip",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Labels.Add(label);
        var member = MakeMember("label@test.com");
        ctx.Members.Add(member);
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        await repo.AddLabelToMemberAsync(member.Id, label.Id);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByIdAsync(member.Id);
        Assert.NotNull(loaded);
        Assert.Single(loaded.MembersLabels);
        Assert.Equal("VIP", loaded.MembersLabels.First().Label.Name);
    }

    [Fact]
    public async Task AddLabelToMemberAsync_Duplicate_DoesNotThrow()
    {
        await using var ctx = db.CreateContext();
        var label = new Label
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Name = "Dup",
            Slug = "dup",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Labels.Add(label);
        var member = MakeMember("dup-label@test.com");
        ctx.Members.Add(member);
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        await repo.AddLabelToMemberAsync(member.Id, label.Id);
        await repo.AddLabelToMemberAsync(member.Id, label.Id); // duplicate — should not throw

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByIdAsync(member.Id);
        Assert.Single(loaded!.MembersLabels);
    }

    [Fact]
    public async Task RemoveLabelFromMemberAsync_RemovesAssociation()
    {
        await using var ctx = db.CreateContext();
        var label = new Label
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Name = "Remove Me",
            Slug = "remove-me",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Labels.Add(label);
        var member = MakeMember("rem-label@test.com");
        ctx.Members.Add(member);
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        await repo.AddLabelToMemberAsync(member.Id, label.Id);
        await repo.RemoveLabelFromMemberAsync(member.Id, label.Id);

        var repo2 = CreateRepo();
        var loaded = await repo2.GetByIdAsync(member.Id);
        Assert.Empty(loaded!.MembersLabels);
    }

    // ── UpdateNote ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateNoteAsync_SetsNote()
    {
        var repo = CreateRepo();
        var member = MakeMember("note@test.com");
        await repo.AddAsync(member);

        var repo2 = CreateRepo();
        await repo2.UpdateNoteAsync(member.Id, "Important member");

        var repo3 = CreateRepo();
        var loaded = await repo3.GetByIdAsync(member.Id);
        Assert.Equal("Important member", loaded!.Note);
    }

    // ── Events ─────────────────────────────────────────────────

    [Fact]
    public async Task AddCreatedEventAsync_PersistsEvent()
    {
        var repo = CreateRepo();
        var member = MakeMember("event@test.com");
        await repo.AddAsync(member);

        var repo2 = CreateRepo();
        await repo2.AddCreatedEventAsync(new MembersCreatedEvent
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            MemberId = member.Id,
            Source = "signup",
            CreatedAt = DateTime.UtcNow,
        });

        await using var ctx = db.CreateContext();
        var events = await ctx.MembersCreatedEvents
            .Where(e => e.MemberId == member.Id)
            .ToListAsync();
        Assert.Single(events);
        Assert.Equal("signup", events[0].Source);
    }

    [Fact]
    public async Task AddStatusEventAsync_PersistsEvent()
    {
        var repo = CreateRepo();
        var member = MakeMember("status-event@test.com");
        await repo.AddAsync(member);

        var repo2 = CreateRepo();
        await repo2.AddStatusEventAsync(new MembersStatusEvent
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            MemberId = member.Id,
            FromStatus = "free",
            ToStatus = "paid",
            CreatedAt = DateTime.UtcNow,
        });

        await using var ctx = db.CreateContext();
        var events = await ctx.MembersStatusEvents
            .Where(e => e.MemberId == member.Id)
            .ToListAsync();
        Assert.Single(events);
    }

    // ── Newsletter subscribers ─────────────────────────────────

    [Fact]
    public async Task GetNewsletterSubscribersAsync_ReturnsSubscribedMembers()
    {
        await using var ctx = db.CreateContext();
        var newsletter = new Newsletter
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Uuid = Guid.NewGuid().ToString("D"),
            Name = "Weekly",
            Slug = "weekly",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Newsletters.Add(newsletter);

        var subscribed = MakeMember("sub@test.com");
        var notSubscribed = MakeMember("nosub@test.com");
        var disabled = MakeMember("disabled@test.com");
        disabled.EmailDisabled = true;
        ctx.Members.AddRange(subscribed, notSubscribed, disabled);
        await ctx.SaveChangesAsync();

        ctx.MembersNewsletters.Add(new MembersNewsletter
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = subscribed.Id,
            NewsletterId = newsletter.Id,
        });
        ctx.MembersNewsletters.Add(new MembersNewsletter
        {
            Id = Guid.NewGuid().ToString("D"),
            MemberId = disabled.Id,
            NewsletterId = newsletter.Id,
        });
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var subscribers = await repo.GetNewsletterSubscribersAsync(newsletter.Id);

        // disabled member should be excluded
        Assert.Single(subscribers);
        Assert.Equal("sub@test.com", subscribers[0].Email);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
