using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Core.Tests;

public class CommentServiceTests
{
    private readonly FakeCommentRepo _commentRepo = new();
    private readonly FakePostRepo _postRepo = new();
    private readonly FakeMemberRepo _memberRepo = new();
    private readonly CommentService _sut;

    private const string PostId = "post-1";
    private const string MemberId = "member-1";

    public CommentServiceTests()
    {
        _sut = new CommentService(
            _commentRepo,
            _postRepo,
            _memberRepo,
            NullLogger<CommentService>.Instance);

        // Seed a published post and a member
        _postRepo.Posts.Add(new Post
        {
            Id = PostId,
            Uuid = "uuid-1",
            Title = "Test Post",
            Slug = "test-post",
            Status = "published",
        });

        _memberRepo.Members.Add(new Member
        {
            Id = MemberId,
            Uuid = "muuid-1",
            TransientId = "tid-1",
            Email = "test@example.com",
            Name = "Alice",
            Status = "free",
            CreatedAt = DateTime.UtcNow,
        });
    }

    // ── AddCommentAsync ─────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_ValidPost_CreatesComment()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Hello</p>");

        Assert.NotNull(comment);
        Assert.Equal(PostId, comment.PostId);
        Assert.Equal(MemberId, comment.MemberId);
        Assert.Equal("<p>Hello</p>", comment.Html);
        Assert.Equal("published", comment.Status);
        Assert.Null(comment.ParentId);
        Assert.Single(_commentRepo.Comments);
    }

    [Fact]
    public async Task AddCommentAsync_WithParent_SetsParentId()
    {
        var parent = await _sut.AddCommentAsync(PostId, MemberId, "<p>Parent</p>");
        var reply = await _sut.AddCommentAsync(PostId, MemberId, "<p>Reply</p>", parent.Id);

        Assert.Equal(parent.Id, reply.ParentId);
        Assert.Equal(2, _commentRepo.Comments.Count);
    }

    [Fact]
    public async Task AddCommentAsync_PostNotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddCommentAsync("nonexistent", MemberId, "<p>Hi</p>"));
    }

    [Fact]
    public async Task AddCommentAsync_UnpublishedPost_Throws()
    {
        _postRepo.Posts[0].Status = "draft";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddCommentAsync(PostId, MemberId, "<p>Hi</p>"));
    }

    [Fact]
    public async Task AddCommentAsync_MemberNotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddCommentAsync(PostId, "nonexistent", "<p>Hi</p>"));
    }

    [Fact]
    public async Task AddCommentAsync_ParentNotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddCommentAsync(PostId, MemberId, "<p>Reply</p>", "bad-parent"));
    }

    [Fact]
    public async Task AddCommentAsync_ParentWrongPost_Throws()
    {
        // Add another post and a comment on it
        _postRepo.Posts.Add(new Post
        {
            Id = "post-2",
            Uuid = "uuid-2",
            Title = "Other",
            Slug = "other",
            Status = "published",
        });
        var otherComment = await _sut.AddCommentAsync("post-2", MemberId, "<p>Other</p>");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddCommentAsync(PostId, MemberId, "<p>Reply</p>", otherComment.Id));
    }

    [Fact]
    public async Task AddCommentAsync_UpdatesMemberLastCommentedAt()
    {
        await _sut.AddCommentAsync(PostId, MemberId, "<p>Hi</p>");

        var member = _memberRepo.Members[0];
        Assert.NotNull(member.LastCommentedAt);
    }

    // ── EditCommentAsync ────────────────────────────────

    [Fact]
    public async Task EditCommentAsync_OwnComment_UpdatesHtml()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Original</p>");

        var edited = await _sut.EditCommentAsync(comment.Id, MemberId, "<p>Updated</p>");

        Assert.Equal("<p>Updated</p>", edited.Html);
        Assert.NotNull(edited.EditedAt);
    }

    [Fact]
    public async Task EditCommentAsync_OtherMember_ThrowsUnauthorized()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Mine</p>");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.EditCommentAsync(comment.Id, "other-member", "<p>Hack</p>"));
    }

    [Fact]
    public async Task EditCommentAsync_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EditCommentAsync("nonexistent", MemberId, "<p>X</p>"));
    }

    [Fact]
    public async Task EditCommentAsync_HiddenComment_Throws()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Test</p>");
        await _sut.HideCommentAsync(comment.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EditCommentAsync(comment.Id, MemberId, "<p>Edit</p>"));
    }

    // ── DeleteCommentAsync ──────────────────────────────

    [Fact]
    public async Task DeleteCommentAsync_OwnComment_RemovesIt()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Bye</p>");

        await _sut.DeleteCommentAsync(comment.Id, MemberId);

        Assert.Empty(_commentRepo.Comments);
    }

    [Fact]
    public async Task DeleteCommentAsync_OtherMember_ThrowsUnauthorized()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Mine</p>");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteCommentAsync(comment.Id, "other-member"));
    }

    // ── LikeCommentAsync / UnlikeCommentAsync ───────────

    [Fact]
    public async Task LikeCommentAsync_CreatesLike()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Likeable</p>");

        var like = await _sut.LikeCommentAsync(comment.Id, MemberId);

        Assert.Equal(comment.Id, like.CommentId);
        Assert.Equal(MemberId, like.MemberId);
        Assert.Single(_commentRepo.Likes);
    }

    [Fact]
    public async Task LikeCommentAsync_Duplicate_ReturnsExisting()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Liked</p>");
        var like1 = await _sut.LikeCommentAsync(comment.Id, MemberId);
        var like2 = await _sut.LikeCommentAsync(comment.Id, MemberId);

        Assert.Equal(like1.Id, like2.Id);
        Assert.Single(_commentRepo.Likes);
    }

    [Fact]
    public async Task UnlikeCommentAsync_RemovesLike()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Unliked</p>");
        await _sut.LikeCommentAsync(comment.Id, MemberId);

        await _sut.UnlikeCommentAsync(comment.Id, MemberId);

        Assert.Empty(_commentRepo.Likes);
    }

    [Fact]
    public async Task LikeCommentAsync_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LikeCommentAsync("nonexistent", MemberId));
    }

    // ── ReportCommentAsync ──────────────────────────────

    [Fact]
    public async Task ReportCommentAsync_CreatesReport()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Bad</p>");

        var report = await _sut.ReportCommentAsync(comment.Id, MemberId);

        Assert.Equal(comment.Id, report.CommentId);
        Assert.Single(_commentRepo.Reports);
    }

    [Fact]
    public async Task ReportCommentAsync_Duplicate_ReturnsExisting()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Bad</p>");
        var r1 = await _sut.ReportCommentAsync(comment.Id, MemberId);
        var r2 = await _sut.ReportCommentAsync(comment.Id, MemberId);

        Assert.Equal(r1.Id, r2.Id);
        Assert.Single(_commentRepo.Reports);
    }

    // ── HideCommentAsync / ApproveCommentAsync ──────────

    [Fact]
    public async Task HideCommentAsync_SetsStatusHidden()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>Spam</p>");

        await _sut.HideCommentAsync(comment.Id);

        Assert.Equal("hidden", _commentRepo.Comments[0].Status);
    }

    [Fact]
    public async Task ApproveCommentAsync_SetsStatusPublished()
    {
        var comment = await _sut.AddCommentAsync(PostId, MemberId, "<p>OK</p>");
        await _sut.HideCommentAsync(comment.Id);

        await _sut.ApproveCommentAsync(comment.Id);

        Assert.Equal("published", _commentRepo.Comments[0].Status);
    }

    [Fact]
    public async Task HideCommentAsync_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HideCommentAsync("nonexistent"));
    }

    // ── Fake implementations ────────────────────────────

    private sealed class FakeCommentRepo : ICommentRepository
    {
        public List<Comment> Comments { get; } = [];
        public List<CommentLike> Likes { get; } = [];
        public List<CommentReport> Reports { get; } = [];

        public Task<Comment?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Comments.FirstOrDefault(c => c.Id == id));

        public Task<PagedResult<Comment>> GetByPostIdAsync(string postId, int page, int pageSize, CancellationToken ct = default)
        {
            var items = Comments.Where(c => c.PostId == postId && c.ParentId == null && c.Status == "published")
                .OrderByDescending(c => c.CreatedAt).ToList();
            return Task.FromResult(new PagedResult<Comment>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = items.Count,
            });
        }

        public Task<PagedResult<Comment>> GetAllAsync(string? status, int page, int pageSize, CancellationToken ct = default)
        {
            var query = Comments.AsEnumerable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);
            var items = query.OrderByDescending(c => c.CreatedAt).ToList();
            return Task.FromResult(new PagedResult<Comment>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = items.Count,
            });
        }

        public Task<List<Comment>> GetRepliesAsync(string parentId, CancellationToken ct = default)
            => Task.FromResult(Comments.Where(c => c.ParentId == parentId && c.Status == "published")
                .OrderBy(c => c.CreatedAt).ToList());

        public Task<int> GetCountByPostIdAsync(string postId, CancellationToken ct = default)
            => Task.FromResult(Comments.Count(c => c.PostId == postId && c.Status == "published"));

        public Task AddAsync(Comment comment, CancellationToken ct = default)
        { Comments.Add(comment); return Task.CompletedTask; }

        public Task UpdateAsync(Comment comment, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Comments.RemoveAll(c => c.Id == id); return Task.CompletedTask; }

        public Task<CommentLike?> GetLikeAsync(string commentId, string memberId, CancellationToken ct = default)
            => Task.FromResult(Likes.FirstOrDefault(l => l.CommentId == commentId && l.MemberId == memberId));

        public Task AddLikeAsync(CommentLike like, CancellationToken ct = default)
        { Likes.Add(like); return Task.CompletedTask; }

        public Task RemoveLikeAsync(string commentId, string memberId, CancellationToken ct = default)
        { Likes.RemoveAll(l => l.CommentId == commentId && l.MemberId == memberId); return Task.CompletedTask; }

        public Task<CommentReport?> GetReportAsync(string commentId, string memberId, CancellationToken ct = default)
            => Task.FromResult(Reports.FirstOrDefault(r => r.CommentId == commentId && r.MemberId == memberId));

        public Task AddReportAsync(CommentReport report, CancellationToken ct = default)
        { Reports.Add(report); return Task.CompletedTask; }
    }

    private sealed class FakePostRepo : IPostRepository
    {
        public List<Post> Posts { get; } = [];

        public Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));
        public Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Slug == slug));
        public Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult<Post?>(null);
        public Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task AddAsync(Post post, CancellationToken ct = default)
        { Posts.Add(post); return Task.CompletedTask; }
        public Task UpdateAsync(Post post, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Posts.RemoveAll(p => p.Id == id); return Task.CompletedTask; }
        public Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default)
            => Task.FromResult(Posts.Count);
        public Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
        { Posts.RemoveAll(p => ids.Contains(p.Id)); return Task.CompletedTask; }
        public Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default)
        { foreach (var p in Posts.Where(p => ids.Contains(p.Id))) p.Featured = featured; return Task.CompletedTask; }
        public Task<List<Post>> GetAllPagesAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetPublishedPagesAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateSortOrderAsync(IReadOnlyList<(string Id, string? ParentId, int SortOrder)> updates, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

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
