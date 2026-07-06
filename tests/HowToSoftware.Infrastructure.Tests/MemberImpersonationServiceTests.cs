using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data.Repositories;
using HowToSoftware.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class MemberImpersonationServiceTests(DatabaseFixture db) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        await ctx.Tokens.ExecuteDeleteAsync();
        await ctx.AdminAuditLogs.ExecuteDeleteAsync();
        await ctx.Members.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private MemberImpersonationService CreateService()
    {
        var ctx = db.CreateContext();
        var repo = new MemberRepository(ctx);
        return new MemberImpersonationService(ctx, repo, NullLogger<MemberImpersonationService>.Instance);
    }

    private async Task<Member> SeedMemberAsync(string email = "fan@example.com")
    {
        await using var ctx = db.CreateContext();
        var m = new Member
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = email,
            Name = "Fan",
            Status = "paid",
            CreatedAt = DateTime.UtcNow,
        };
        ctx.Members.Add(m);
        await ctx.SaveChangesAsync();
        return m;
    }

    [Fact]
    public async Task CreateTokenAsync_ReturnsNonEmptyRawTokenAndPersistsHashedRow()
    {
        var member = await SeedMemberAsync();

        var raw = await CreateService().CreateTokenAsync(member.Id, "admin-1", "admin@example.com");

        Assert.False(string.IsNullOrWhiteSpace(raw));
        Assert.True(raw.Length >= 32, "raw token should be reasonably long");

        await using var ctx = db.CreateContext();
        var stored = await ctx.Tokens.SingleAsync();
        Assert.NotEqual(raw, stored.TokenValue); // hashed, not raw
        Assert.Contains("admin-impersonate", stored.Data);
        Assert.Contains(member.Id, stored.Data);
        Assert.Equal(0, stored.UsedCount);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_ReturnsMemberAndAdminInfoOnValidToken()
    {
        var member = await SeedMemberAsync();
        var raw = await CreateService().CreateTokenAsync(member.Id, "admin-1", "admin@example.com");

        var result = await CreateService().VerifyAndConsumeAsync(raw);

        Assert.NotNull(result);
        Assert.Equal(member.Id, result!.Member.Id);
        Assert.Equal("admin-1", result.AdminUserId);
        Assert.Equal("admin@example.com", result.AdminUserEmail);

        await using var ctx = db.CreateContext();
        var token = await ctx.Tokens.SingleAsync();
        Assert.Equal(1, token.UsedCount);
        Assert.NotNull(token.FirstUsedAt);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_RejectsReusedToken()
    {
        var member = await SeedMemberAsync();
        var raw = await CreateService().CreateTokenAsync(member.Id, "admin-1", null);

        var first = await CreateService().VerifyAndConsumeAsync(raw);
        Assert.NotNull(first);

        var second = await CreateService().VerifyAndConsumeAsync(raw);
        Assert.Null(second);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_RejectsExpiredToken()
    {
        var member = await SeedMemberAsync();
        var raw = await CreateService().CreateTokenAsync(member.Id, "admin-1", null);

        // Backdate the token past the lifetime.
        await using (var ctx = db.CreateContext())
        {
            await ctx.Tokens.ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedAt, DateTime.UtcNow.AddMinutes(-10)));
        }

        var result = await CreateService().VerifyAndConsumeAsync(raw);
        Assert.Null(result);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_RejectsUnknownToken()
    {
        var result = await CreateService().VerifyAndConsumeAsync("not-a-real-token");
        Assert.Null(result);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_RejectsEmptyToken()
    {
        var result = await CreateService().VerifyAndConsumeAsync("");
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTokenAsync_ThrowsWhenMemberMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().CreateTokenAsync("does-not-exist", "admin-1", null));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
