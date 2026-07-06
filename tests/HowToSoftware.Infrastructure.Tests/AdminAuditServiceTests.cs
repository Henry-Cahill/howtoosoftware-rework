using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Infrastructure.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class AdminAuditServiceTests(DatabaseFixture db) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var ctx = db.CreateContext();
        await ctx.AdminAuditLogs.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private AdminAuditService CreateService() =>
        new(db.CreateContext(), NullLogger<AdminAuditService>.Instance);

    [Fact]
    public async Task LogAsync_PersistsEntry()
    {
        await CreateService().LogAsync(new AdminAuditEntry
        {
            AdminUserId = "admin-1",
            AdminUserEmail = "admin@example.com",
            Action = "member.impersonate.start",
            TargetType = "member",
            TargetId = "member-1",
            Metadata = "{\"foo\":\"bar\"}",
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
        });

        await using var ctx = db.CreateContext();
        var row = await ctx.AdminAuditLogs.SingleAsync();
        Assert.Equal("admin-1", row.AdminUserId);
        Assert.Equal("member.impersonate.start", row.Action);
        Assert.Equal("member", row.TargetType);
        Assert.Equal("member-1", row.TargetId);
        Assert.Equal("127.0.0.1", row.IpAddress);
        Assert.Equal("test-agent", row.UserAgent);
        Assert.NotEqual(default, row.CreatedAt);
    }

    [Fact]
    public async Task LogAsync_TruncatesOverlongUserAgent()
    {
        var longUa = new string('x', 1000);
        await CreateService().LogAsync(new AdminAuditEntry
        {
            AdminUserId = "admin-1",
            Action = "test.action",
            UserAgent = longUa,
        });

        await using var ctx = db.CreateContext();
        var row = await ctx.AdminAuditLogs.SingleAsync();
        Assert.NotNull(row.UserAgent);
        Assert.True(row.UserAgent!.Length <= 512);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
