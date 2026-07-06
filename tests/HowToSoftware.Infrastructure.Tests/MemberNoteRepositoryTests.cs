using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class MemberNoteRepositoryTests(DatabaseFixture db) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        await ctx.MemberNotes.ExecuteDeleteAsync();
        await ctx.Members.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private MemberNoteRepository CreateRepo() => new(db.CreateContext());

    private async Task<Member> SeedMemberAsync(string email)
    {
        await using var ctx = db.CreateContext();
        var m = new Member
        {
            Id = ObjectIdGenerator.New(),
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = email,
            Status = "free",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Members.Add(m);
        await ctx.SaveChangesAsync();
        return m;
    }

    private static MemberNote MakeNote(string memberId, string body, DateTime? createdAt = null) => new()
    {
        Id = ObjectIdGenerator.New(),
        MemberId = memberId,
        AuthorId = null,
        AuthorName = "Admin",
        Body = body,
        CreatedAt = createdAt ?? DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_PersistsNote()
    {
        var member = await SeedMemberAsync("note1@example.com");
        var note = MakeNote(member.Id, "First note");

        await CreateRepo().AddAsync(note);

        var loaded = await CreateRepo().GetByMemberAsync(member.Id);
        var single = Assert.Single(loaded);
        Assert.Equal("First note", single.Body);
        Assert.Equal("Admin", single.AuthorName);
    }

    [Fact]
    public async Task GetByMemberAsync_OrdersOldestFirst()
    {
        var member = await SeedMemberAsync("note2@example.com");
        var t0 = DateTime.UtcNow.AddMinutes(-30);
        await CreateRepo().AddAsync(MakeNote(member.Id, "second", t0.AddMinutes(10)));
        await CreateRepo().AddAsync(MakeNote(member.Id, "third", t0.AddMinutes(20)));
        await CreateRepo().AddAsync(MakeNote(member.Id, "first", t0));

        var loaded = await CreateRepo().GetByMemberAsync(member.Id);

        Assert.Equal(3, loaded.Count);
        Assert.Equal("first", loaded[0].Body);
        Assert.Equal("second", loaded[1].Body);
        Assert.Equal("third", loaded[2].Body);
    }

    [Fact]
    public async Task GetByMemberAsync_OnlyReturnsRowsForRequestedMember()
    {
        var m1 = await SeedMemberAsync("a@example.com");
        var m2 = await SeedMemberAsync("b@example.com");

        await CreateRepo().AddAsync(MakeNote(m1.Id, "for-m1"));
        await CreateRepo().AddAsync(MakeNote(m2.Id, "for-m2"));

        var loaded = await CreateRepo().GetByMemberAsync(m1.Id);
        var single = Assert.Single(loaded);
        Assert.Equal("for-m1", single.Body);
    }

    [Fact]
    public async Task GetByMemberAsync_ReturnsEmptyWhenNoNotes()
    {
        var member = await SeedMemberAsync("empty@example.com");

        var loaded = await CreateRepo().GetByMemberAsync(member.Id);

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task DeleteMember_CascadesToNotes()
    {
        var member = await SeedMemberAsync("cascade@example.com");
        await CreateRepo().AddAsync(MakeNote(member.Id, "doomed"));

        await using (var ctx = db.CreateContext())
        {
            await ctx.Members.Where(m => m.Id == member.Id).ExecuteDeleteAsync();
        }

        await using var verify = db.CreateContext();
        var remaining = await verify.MemberNotes.CountAsync(n => n.MemberId == member.Id);
        Assert.Equal(0, remaining);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
