using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Tests.Fakes;

internal class FakeCommentService : ICommentService
{
    public List<Comment> Comments { get; } = [];
    public List<CommentLike> Likes { get; } = [];
    public List<CommentReport> Reports { get; } = [];

    public Task<Comment> AddCommentAsync(string postId, string memberId, string html, string? parentId = null, CancellationToken ct = default)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            PostId = postId,
            MemberId = memberId,
            Html = html,
            ParentId = parentId,
            Status = "published",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Member = new Member { Id = memberId, Uuid = Guid.NewGuid().ToString(), TransientId = "t", Email = "test@test.com" },
        };
        Comments.Add(comment);
        return Task.FromResult(comment);
    }

    public Task<Comment> EditCommentAsync(string commentId, string memberId, string html, CancellationToken ct = default)
    {
        var comment = Comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new InvalidOperationException("Comment not found.");
        if (comment.MemberId != memberId)
            throw new UnauthorizedAccessException();
        comment.Html = html;
        comment.EditedAt = DateTime.UtcNow;
        return Task.FromResult(comment);
    }

    public Task DeleteCommentAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        var comment = Comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new InvalidOperationException("Comment not found.");
        if (comment.MemberId != memberId)
            throw new UnauthorizedAccessException();
        Comments.Remove(comment);
        return Task.CompletedTask;
    }

    public Task<CommentLike> LikeCommentAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        var like = new CommentLike { Id = Guid.NewGuid().ToString("N")[..24], CommentId = commentId, MemberId = memberId, CreatedAt = DateTime.UtcNow };
        Likes.Add(like);
        return Task.FromResult(like);
    }

    public Task UnlikeCommentAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        Likes.RemoveAll(l => l.CommentId == commentId && l.MemberId == memberId);
        return Task.CompletedTask;
    }

    public Task<CommentReport> ReportCommentAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        var report = new CommentReport { Id = Guid.NewGuid().ToString("N")[..24], CommentId = commentId, MemberId = memberId, CreatedAt = DateTime.UtcNow };
        Reports.Add(report);
        return Task.FromResult(report);
    }

    public Task HideCommentAsync(string commentId, CancellationToken ct = default) => Task.CompletedTask;
    public Task ApproveCommentAsync(string commentId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<PagedResult<Comment>> GetCommentsForPostAsync(string postId, int page, int pageSize, CancellationToken ct = default)
    {
        var all = Comments.Where(c => c.PostId == postId && c.ParentId == null).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<Comment>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = all.Count,
        });
    }

    public Task<List<Comment>> GetRepliesAsync(string parentId, CancellationToken ct = default)
        => Task.FromResult(Comments.Where(c => c.ParentId == parentId).ToList());

    public Task<int> GetCommentCountAsync(string postId, CancellationToken ct = default)
        => Task.FromResult(Comments.Count(c => c.PostId == postId));

    public Task<PagedResult<Comment>> GetAllCommentsAsync(string? status, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<Comment> { Items = [], Page = page, PageSize = pageSize, TotalCount = 0 });
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
