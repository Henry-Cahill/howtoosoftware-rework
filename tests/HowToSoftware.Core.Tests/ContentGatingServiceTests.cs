using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class ContentGatingServiceTests
{
    private readonly FakeMemberRepo _memberRepo = new();
    private readonly ContentGatingService _sut;

    public ContentGatingServiceTests()
    {
        _sut = new ContentGatingService(_memberRepo);
    }

    // ── Public posts ────────────────────────────────────────────

    [Fact]
    public async Task PublicPost_Anonymous_FullAccess()
    {
        var post = MakePost(visibility: "public");
        var result = await _sut.CheckAccessAsync(post, memberId: null);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    [Fact]
    public async Task PublicPost_FreeMember_FullAccess()
    {
        var member = AddMember("m1", status: "free");
        var post = MakePost(visibility: "public");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    // ── Members-only posts ──────────────────────────────────────

    [Fact]
    public async Task MembersPost_Anonymous_RequiresMember()
    {
        var post = MakePost(visibility: "members");
        var result = await _sut.CheckAccessAsync(post, memberId: null);
        Assert.Equal(ContentAccessLevel.RequiresMember, result);
    }

    [Fact]
    public async Task MembersPost_FreeMember_FullAccess()
    {
        var member = AddMember("m1", status: "free");
        var post = MakePost(visibility: "members");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    [Fact]
    public async Task MembersPost_PaidMember_FullAccess()
    {
        var member = AddMember("m1", status: "paid");
        var post = MakePost(visibility: "members");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    // ── Paid-only posts ─────────────────────────────────────────

    [Fact]
    public async Task PaidPost_Anonymous_RequiresPaid()
    {
        var post = MakePost(visibility: "paid");
        var result = await _sut.CheckAccessAsync(post, memberId: null);
        Assert.Equal(ContentAccessLevel.RequiresPaid, result);
    }

    [Fact]
    public async Task PaidPost_FreeMember_RequiresPaid()
    {
        var member = AddMember("m1", status: "free");
        var post = MakePost(visibility: "paid");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.RequiresPaid, result);
    }

    [Fact]
    public async Task PaidPost_PaidMember_FullAccess()
    {
        var member = AddMember("m1", status: "paid");
        var post = MakePost(visibility: "paid");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    [Fact]
    public async Task PaidPost_CompedMember_FullAccess()
    {
        var member = AddMember("m1", status: "comped");
        var post = MakePost(visibility: "paid");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    // ── Tier-gated posts ────────────────────────────────────────

    [Fact]
    public async Task TiersPost_Anonymous_RequiresTier()
    {
        var post = MakePost(visibility: "tiers", productIds: ["prod1"]);
        var result = await _sut.CheckAccessAsync(post, memberId: null);
        Assert.Equal(ContentAccessLevel.RequiresTier, result);
    }

    [Fact]
    public async Task TiersPost_MemberWithoutProduct_RequiresTier()
    {
        var member = AddMember("m1", status: "paid");
        var post = MakePost(visibility: "tiers", productIds: ["prod1"]);

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.RequiresTier, result);
    }

    [Fact]
    public async Task TiersPost_MemberWithMatchingProduct_FullAccess()
    {
        var member = AddMember("m1", status: "paid", productIds: ["prod1"]);
        var post = MakePost(visibility: "tiers", productIds: ["prod1", "prod2"]);

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    [Fact]
    public async Task TiersPost_MemberWithExpiredProduct_RequiresTier()
    {
        var member = AddMember("m1", status: "paid", productIds: ["prod1"],
            expiryAt: DateTime.UtcNow.AddDays(-1));
        var post = MakePost(visibility: "tiers", productIds: ["prod1"]);

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.RequiresTier, result);
    }

    [Fact]
    public async Task TiersPost_MemberWithFutureExpiry_FullAccess()
    {
        var member = AddMember("m1", status: "paid", productIds: ["prod1"],
            expiryAt: DateTime.UtcNow.AddDays(30));
        var post = MakePost(visibility: "tiers", productIds: ["prod1"]);

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    [Fact]
    public async Task TiersPost_NoRequiredProducts_FullAccess()
    {
        // Edge case: tiers visibility but no products attached means accessible
        var member = AddMember("m1", status: "free");
        var post = MakePost(visibility: "tiers");

        var result = await _sut.CheckAccessAsync(post, member.Id);
        Assert.Equal(ContentAccessLevel.Full, result);
    }

    // ── Unknown member ID ───────────────────────────────────────

    [Fact]
    public async Task MembersPost_InvalidMemberId_RequiresMember()
    {
        var post = MakePost(visibility: "members");
        var result = await _sut.CheckAccessAsync(post, memberId: "nonexistent");
        Assert.Equal(ContentAccessLevel.RequiresMember, result);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static Post MakePost(string visibility, string[]? productIds = null)
    {
        var post = new Post
        {
            Id = Guid.NewGuid().ToString("N"),
            Uuid = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Slug = "test-post",
            Visibility = visibility,
            Status = "published",
            CreatedAt = DateTime.UtcNow,
        };

        if (productIds is not null)
        {
            foreach (var pid in productIds)
            {
                post.PostsProducts.Add(new PostsProduct
                {
                    Id = Guid.NewGuid().ToString("N"),
                    PostId = post.Id,
                    ProductId = pid,
                });
            }
        }

        return post;
    }

    private Member AddMember(string id, string status,
        string[]? productIds = null, DateTime? expiryAt = null)
    {
        var member = new Member
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString(),
            TransientId = Guid.NewGuid().ToString(),
            Email = $"{id}@example.com",
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };

        if (productIds is not null)
        {
            foreach (var pid in productIds)
            {
                member.MembersProducts.Add(new MembersProduct
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MemberId = member.Id,
                    ProductId = pid,
                    ExpiryAt = expiryAt,
                });
            }
        }

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
            => Task.FromResult(Members.FirstOrDefault(m => m.Email == email));
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
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
