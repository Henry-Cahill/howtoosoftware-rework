using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

public class MentionServiceTests
{
    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakeMentionService : IMentionService
    {
        public List<Mention> Mentions { get; } = [];

        public Task<List<Mention>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Mentions.Where(m => !m.Deleted).OrderByDescending(m => m.CreatedAt).ToList());

        public Task<int> GetPendingCountAsync(CancellationToken ct = default)
            => Task.FromResult(Mentions.Count(m => !m.Deleted && m.Status == MentionStatus.Pending));

        public Task<Mention?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Mentions.FirstOrDefault(m => m.Id == id));

        public Task<List<Mention>> GetByTargetAsync(string targetUrl, CancellationToken ct = default)
            => Task.FromResult(Mentions.Where(m => m.Target == targetUrl && m.Verified && !m.Deleted && m.Status == MentionStatus.Approved)
                .OrderByDescending(m => m.CreatedAt).ToList());

        public Task<List<Mention>> GetByResourceAsync(string resourceId, string resourceType, CancellationToken ct = default)
            => Task.FromResult(Mentions.Where(m => m.ResourceId == resourceId && m.ResourceType == resourceType && m.Verified && !m.Deleted && m.Status == MentionStatus.Approved)
                .OrderByDescending(m => m.CreatedAt).ToList());

        public Task<Mention> ReceiveAsync(string source, string target, CancellationToken ct = default)
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
                || (sourceUri.Scheme != "http" && sourceUri.Scheme != "https"))
                throw new ArgumentException("Invalid source URL");

            if (!Uri.TryCreate(target, UriKind.Absolute, out var targetUri)
                || (targetUri.Scheme != "http" && targetUri.Scheme != "https"))
                throw new ArgumentException("Invalid target URL");

            if (sourceUri == targetUri)
                throw new ArgumentException("Source and target must be different URLs");

            var existing = Mentions.FirstOrDefault(m => m.Source == source && m.Target == target);
            if (existing is not null)
            {
                existing.Deleted = false;
                return Task.FromResult(existing);
            }

            var mention = new Mention
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                Source = source,
                Target = target,
                CreatedAt = DateTime.UtcNow,
                Verified = false,
                Deleted = false,
                Status = MentionStatus.Pending,
            };
            Mentions.Add(mention);
            return Task.FromResult(mention);
        }

        public Task VerifyAsync(string id, CancellationToken ct = default)
        {
            var mention = Mentions.FirstOrDefault(m => m.Id == id)
                ?? throw new InvalidOperationException($"Mention {id} not found");
            mention.Verified = true;
            return Task.CompletedTask;
        }

        public Task ApproveAsync(string id, CancellationToken ct = default)
        {
            var mention = Mentions.FirstOrDefault(m => m.Id == id)
                ?? throw new InvalidOperationException($"Mention {id} not found");
            mention.Status = MentionStatus.Approved;
            return Task.CompletedTask;
        }

        public Task RejectAsync(string id, CancellationToken ct = default)
        {
            var mention = Mentions.FirstOrDefault(m => m.Id == id)
                ?? throw new InvalidOperationException($"Mention {id} not found");
            mention.Status = MentionStatus.Rejected;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var mention = Mentions.FirstOrDefault(m => m.Id == id)
                ?? throw new InvalidOperationException($"Mention {id} not found");
            mention.Deleted = true;
            return Task.CompletedTask;
        }
    }

    // ── Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task ReceiveAsync_CreatesNewMention()
    {
        var sut = new FakeMentionService();

        var mention = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        Assert.NotNull(mention);
        Assert.Equal("https://example.com/post", mention.Source);
        Assert.Equal("https://mysite.com/article", mention.Target);
        Assert.False(mention.Deleted);
        Assert.Single(sut.Mentions);
    }

    [Fact]
    public async Task ReceiveAsync_InvalidSourceUrl_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ReceiveAsync("not-a-url", "https://mysite.com/article"));
    }

    [Fact]
    public async Task ReceiveAsync_InvalidTargetUrl_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ReceiveAsync("https://example.com/post", "not-a-url"));
    }

    [Fact]
    public async Task ReceiveAsync_SameSourceAndTarget_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ReceiveAsync("https://example.com/post", "https://example.com/post"));
    }

    [Fact]
    public async Task ReceiveAsync_DuplicateSourceTarget_UpdatesExisting()
    {
        var sut = new FakeMentionService();

        var first = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");
        var second = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        Assert.Equal(first.Id, second.Id);
        Assert.Single(sut.Mentions);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesMention()
    {
        var sut = new FakeMentionService();
        var mention = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        await sut.DeleteAsync(mention.Id);

        Assert.True(sut.Mentions.First().Deleted);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.DeleteAsync("nonexistent"));
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedMentions()
    {
        var sut = new FakeMentionService();
        var m1 = await sut.ReceiveAsync("https://a.com/1", "https://mysite.com/article");
        var m2 = await sut.ReceiveAsync("https://b.com/2", "https://mysite.com/article");
        await sut.DeleteAsync(m1.Id);

        var all = await sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal(m2.Id, all[0].Id);
    }

    [Fact]
    public async Task GetByTargetAsync_ReturnsOnlyVerifiedNonDeleted()
    {
        var sut = new FakeMentionService();
        var m1 = await sut.ReceiveAsync("https://a.com/1", "https://mysite.com/article");
        var m2 = await sut.ReceiveAsync("https://b.com/2", "https://mysite.com/article");
        await sut.VerifyAsync(m1.Id);
        await sut.ApproveAsync(m1.Id);

        var byTarget = await sut.GetByTargetAsync("https://mysite.com/article");

        Assert.Single(byTarget);
        Assert.Equal(m1.Id, byTarget[0].Id);
    }

    [Fact]
    public async Task GetByResourceAsync_ReturnsOnlyVerifiedNonDeleted()
    {
        var sut = new FakeMentionService();
        sut.Mentions.Add(new Mention
        {
            Id = "abc123",
            Source = "https://a.com/1",
            Target = "https://mysite.com/article",
            ResourceId = "post123",
            ResourceType = "post",
            Verified = true,
            Status = MentionStatus.Approved,
            CreatedAt = DateTime.UtcNow,
        });
        sut.Mentions.Add(new Mention
        {
            Id = "def456",
            Source = "https://b.com/2",
            Target = "https://mysite.com/article",
            ResourceId = "post123",
            ResourceType = "post",
            Verified = false,
            Status = MentionStatus.Approved,
            CreatedAt = DateTime.UtcNow,
        });

        var byResource = await sut.GetByResourceAsync("post123", "post");

        Assert.Single(byResource);
        Assert.Equal("abc123", byResource[0].Id);
    }

    [Fact]
    public async Task VerifyAsync_SetsMentionVerified()
    {
        var sut = new FakeMentionService();
        var mention = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        await sut.VerifyAsync(mention.Id);

        Assert.True(sut.Mentions.First().Verified);
    }

    [Fact]
    public async Task VerifyAsync_NotFound_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.VerifyAsync("nonexistent"));
    }

    [Fact]
    public async Task ReceiveAsync_FtpScheme_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ReceiveAsync("ftp://example.com/file", "https://mysite.com/article"));
    }

    // ── Approval workflow (MENT.2) ──────────────────────────────

    [Fact]
    public async Task ReceiveAsync_NewMention_StartsAsPending()
    {
        var sut = new FakeMentionService();

        var mention = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        Assert.Equal(MentionStatus.Pending, mention.Status);
    }

    [Fact]
    public async Task ApproveAsync_SetsStatusApproved()
    {
        var sut = new FakeMentionService();
        var mention = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        await sut.ApproveAsync(mention.Id);

        Assert.Equal(MentionStatus.Approved, sut.Mentions.First().Status);
    }

    [Fact]
    public async Task RejectAsync_SetsStatusRejected()
    {
        var sut = new FakeMentionService();
        var mention = await sut.ReceiveAsync("https://example.com/post", "https://mysite.com/article");

        await sut.RejectAsync(mention.Id);

        Assert.Equal(MentionStatus.Rejected, sut.Mentions.First().Status);
    }

    [Fact]
    public async Task ApproveAsync_NotFound_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApproveAsync("nonexistent"));
    }

    [Fact]
    public async Task RejectAsync_NotFound_Throws()
    {
        var sut = new FakeMentionService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RejectAsync("nonexistent"));
    }

    [Fact]
    public async Task GetByTargetAsync_ExcludesPendingAndRejected()
    {
        var sut = new FakeMentionService();
        var pending = await sut.ReceiveAsync("https://a.com/1", "https://mysite.com/article");
        var rejected = await sut.ReceiveAsync("https://b.com/2", "https://mysite.com/article");
        var approved = await sut.ReceiveAsync("https://c.com/3", "https://mysite.com/article");
        await sut.VerifyAsync(pending.Id);
        await sut.VerifyAsync(rejected.Id);
        await sut.VerifyAsync(approved.Id);
        await sut.RejectAsync(rejected.Id);
        await sut.ApproveAsync(approved.Id);

        var byTarget = await sut.GetByTargetAsync("https://mysite.com/article");

        Assert.Single(byTarget);
        Assert.Equal(approved.Id, byTarget[0].Id);
    }

    // ── Pending count for nav badge (MENT.3) ────────────────────

    [Fact]
    public async Task GetPendingCountAsync_CountsOnlyPendingNonDeleted()
    {
        var sut = new FakeMentionService();
        var p1 = await sut.ReceiveAsync("https://a.com/1", "https://mysite.com/article");
        var p2 = await sut.ReceiveAsync("https://b.com/2", "https://mysite.com/article");
        var approved = await sut.ReceiveAsync("https://c.com/3", "https://mysite.com/article");
        var rejected = await sut.ReceiveAsync("https://d.com/4", "https://mysite.com/article");
        var deleted = await sut.ReceiveAsync("https://e.com/5", "https://mysite.com/article");
        await sut.ApproveAsync(approved.Id);
        await sut.RejectAsync(rejected.Id);
        await sut.DeleteAsync(deleted.Id);

        var count = await sut.GetPendingCountAsync();

        // p1 and p2 are still pending; the others are excluded.
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetPendingCountAsync_NoMentions_ReturnsZero()
    {
        var sut = new FakeMentionService();

        var count = await sut.GetPendingCountAsync();

        Assert.Equal(0, count);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
