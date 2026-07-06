using HowToSoftware.Core.Entities;
using HowToSoftware.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class SettingsRepositoryTests(DatabaseFixture db) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        await ctx.Settings.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private SettingsRepository CreateRepo() => new(db.CreateContext());

    private static Setting MakeSetting(string key, string? value = "val", string group = "core") => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Group = group,
        Key = key,
        Value = value,
        Type = "string",
        CreatedAt = DateTime.UtcNow,
    };

    // ── GetByKeyAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByKeyAsync_ExistingKey_ReturnsSetting()
    {
        await using var ctx = db.CreateContext();
        ctx.Settings.Add(MakeSetting("site_title", "HowToSoftware"));
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var result = await repo.GetByKeyAsync("site_title");

        Assert.NotNull(result);
        Assert.Equal("HowToSoftware", result.Value);
    }

    [Fact]
    public async Task GetByKeyAsync_MissingKey_ReturnsNull()
    {
        var repo = CreateRepo();
        var result = await repo.GetByKeyAsync("nonexistent_key");

        Assert.Null(result);
    }

    // ── GetAllAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllSettingsOrdered()
    {
        await using var ctx = db.CreateContext();
        ctx.Settings.Add(MakeSetting("z_key", "z", "site"));
        ctx.Settings.Add(MakeSetting("a_key", "a", "core"));
        ctx.Settings.Add(MakeSetting("m_key", "m", "core"));
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var result = await repo.GetAllAsync();

        Assert.Equal(3, result.Count);
        // Ordered by group then key
        Assert.Equal("a_key", result[0].Key);
        Assert.Equal("m_key", result[1].Key);
        Assert.Equal("z_key", result[2].Key);
    }

    // ── GetByGroupAsync ────────────────────────────────────────

    [Fact]
    public async Task GetByGroupAsync_FiltersCorrectly()
    {
        await using var ctx = db.CreateContext();
        ctx.Settings.Add(MakeSetting("core_key", "1", "core"));
        ctx.Settings.Add(MakeSetting("site_key", "2", "site"));
        ctx.Settings.Add(MakeSetting("core_key2", "3", "core"));
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        var result = await repo.GetByGroupAsync("core");

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal("core", s.Group));
    }

    // ── UpsertAsync ────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_NewKey_CreatesSetting()
    {
        var repo = CreateRepo();
        await repo.UpsertAsync("new_key", "new_value");

        var repo2 = CreateRepo();
        var result = await repo2.GetByKeyAsync("new_key");

        Assert.NotNull(result);
        Assert.Equal("new_value", result.Value);
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_UpdatesValue()
    {
        await using var ctx = db.CreateContext();
        ctx.Settings.Add(MakeSetting("existing_key", "old_value"));
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        await repo.UpsertAsync("existing_key", "new_value");

        var repo2 = CreateRepo();
        var result = await repo2.GetByKeyAsync("existing_key");

        Assert.NotNull(result);
        Assert.Equal("new_value", result.Value);
    }

    [Fact]
    public async Task UpsertAsync_NullValue_SetsNull()
    {
        var repo = CreateRepo();
        await repo.UpsertAsync("null_key", null);

        var repo2 = CreateRepo();
        var result = await repo2.GetByKeyAsync("null_key");

        Assert.NotNull(result);
        Assert.Null(result.Value);
    }

    // ── DeleteAsync ────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesSetting()
    {
        await using var ctx = db.CreateContext();
        ctx.Settings.Add(MakeSetting("to_delete", "val"));
        await ctx.SaveChangesAsync();

        var repo = CreateRepo();
        await repo.DeleteAsync("to_delete");

        var repo2 = CreateRepo();
        var result = await repo2.GetByKeyAsync("to_delete");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NonexistentKey_DoesNotThrow()
    {
        var repo = CreateRepo();
        await repo.DeleteAsync("does_not_exist"); // Should not throw
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
