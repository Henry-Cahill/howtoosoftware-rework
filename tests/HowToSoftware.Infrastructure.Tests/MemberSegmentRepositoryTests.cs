using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class MemberSegmentRepositoryTests(DatabaseFixture db) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        await ctx.MemberSegments.ExecuteDeleteAsync();
        await ctx.Labels.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private MemberSegmentRepository CreateRepo() => new(db.CreateContext());

    private static MemberSegment MakeSegment(string name, int sortOrder = 0) => new()
    {
        Id = ObjectIdGenerator.New(),
        Name = name,
        StatusFilter = "free",
        EngagementFilter = "ActiveLast30Days",
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_RoundTrips()
    {
        var repo = CreateRepo();
        var seg = MakeSegment("Engaged Free");
        await repo.AddAsync(seg);

        var loaded = await CreateRepo().GetByIdAsync(seg.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Engaged Free", loaded.Name);
        Assert.Equal("free", loaded.StatusFilter);
        Assert.Equal("ActiveLast30Days", loaded.EngagementFilter);
    }

    [Fact]
    public async Task GetAllAsync_OrdersBySortOrderThenName()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeSegment("Zebra", sortOrder: 0));
        await repo.AddAsync(MakeSegment("Alpha", sortOrder: 0));
        await repo.AddAsync(MakeSegment("Middle", sortOrder: -1));

        var all = await CreateRepo().GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("Middle", all[0].Name); // sortOrder -1 first
        Assert.Equal("Alpha", all[1].Name);
        Assert.Equal("Zebra", all[2].Name);
    }

    [Fact]
    public async Task AddAsync_UniqueNameConstraint()
    {
        var repo = CreateRepo();
        await repo.AddAsync(MakeSegment("Duplicate"));

        var dup = MakeSegment("Duplicate");
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => CreateRepo().AddAsync(dup));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesAndStampsUpdatedAt()
    {
        var repo = CreateRepo();
        var seg = MakeSegment("Original");
        await repo.AddAsync(seg);

        var fresh = await CreateRepo().GetByIdAsync(seg.Id);
        Assert.NotNull(fresh);
        fresh.Name = "Renamed";
        fresh.SearchQuery = "alice";
        await CreateRepo().UpdateAsync(fresh);

        var reloaded = await CreateRepo().GetByIdAsync(seg.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Renamed", reloaded.Name);
        Assert.Equal("alice", reloaded.SearchQuery);
        Assert.NotNull(reloaded.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSegment()
    {
        var repo = CreateRepo();
        var seg = MakeSegment("ToDelete");
        await repo.AddAsync(seg);

        await CreateRepo().DeleteAsync(seg.Id);

        var gone = await CreateRepo().GetByIdAsync(seg.Id);
        Assert.Null(gone);
    }

    [Fact]
    public async Task LabelFk_SetNull_OnLabelDelete()
    {
        await using var setup = db.CreateContext();
        var label = new Label
        {
            Id = ObjectIdGenerator.New(),
            Name = "vip",
            Slug = "vip",
            CreatedAt = DateTime.UtcNow,
        };
        setup.Labels.Add(label);
        await setup.SaveChangesAsync();

        var seg = MakeSegment("VIPs");
        seg.LabelId = label.Id;
        await CreateRepo().AddAsync(seg);

        await using (var ctx = db.CreateContext())
        {
            await ctx.Labels.Where(l => l.Id == label.Id).ExecuteDeleteAsync();
        }

        var reloaded = await CreateRepo().GetByIdAsync(seg.Id);
        Assert.NotNull(reloaded);
        Assert.Null(reloaded.LabelId);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
