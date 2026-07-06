using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Core.Services;

public sealed class CommentService(
    ICommentRepository comments,
    IPostRepository posts,
    IMemberRepository members,
    ILogger<CommentService> logger) : ICommentService
{
    public async Task<Comment> AddCommentAsync(
        string postId, string memberId, string html, string? parentId = null, CancellationToken ct = default)
    {
        // Validate post exists and is published
        var post = await posts.GetByIdAsync(postId, ct)
            ?? throw new InvalidOperationException("Post not found.");

        if (post.Status != "published")
            throw new InvalidOperationException("Cannot comment on unpublished posts.");

        // Validate member exists
        var member = await members.GetByIdAsync(memberId, ct)
            ?? throw new InvalidOperationException("Member not found.");

        // If replying, validate parent exists
        if (parentId is not null)
        {
            var parent = await comments.GetByIdAsync(parentId, ct)
                ?? throw new InvalidOperationException("Parent comment not found.");

            if (parent.PostId != postId)
                throw new InvalidOperationException("Parent comment does not belong to this post.");
        }

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            Id = ObjectIdGenerator.New(),
            PostId = postId,
            MemberId = memberId,
            ParentId = parentId,
            Status = "published",
            Html = html,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await comments.AddAsync(comment, ct);

        // Update member's last commented timestamp
        member.LastCommentedAt = now;
        await members.UpdateAsync(member, ct);

        logger.LogInformation("Comment {CommentId} added to post {PostId} by member {MemberId}",
            comment.Id, postId, memberId);

        return comment;
    }

    public async Task<Comment> EditCommentAsync(
        string commentId, string memberId, string html, CancellationToken ct = default)
    {
        var comment = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        if (comment.MemberId != memberId)
            throw new UnauthorizedAccessException("You can only edit your own comments.");

        if (comment.Status != "published")
            throw new InvalidOperationException("Cannot edit a hidden comment.");

        comment.Html = html;
        comment.EditedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        await comments.UpdateAsync(comment, ct);

        logger.LogInformation("Comment {CommentId} edited by member {MemberId}", commentId, memberId);

        return comment;
    }

    public async Task DeleteCommentAsync(
        string commentId, string memberId, CancellationToken ct = default)
    {
        var comment = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        if (comment.MemberId != memberId)
            throw new UnauthorizedAccessException("You can only delete your own comments.");

        await comments.DeleteAsync(commentId, ct);

        logger.LogInformation("Comment {CommentId} deleted by member {MemberId}", commentId, memberId);
    }

    public async Task<CommentLike> LikeCommentAsync(
        string commentId, string memberId, CancellationToken ct = default)
    {
        var comment = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        // Check for duplicate like
        var existing = await comments.GetLikeAsync(commentId, memberId, ct);
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var like = new CommentLike
        {
            Id = ObjectIdGenerator.New(),
            CommentId = commentId,
            MemberId = memberId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await comments.AddLikeAsync(like, ct);

        logger.LogInformation("Comment {CommentId} liked by member {MemberId}", commentId, memberId);

        return like;
    }

    public async Task UnlikeCommentAsync(
        string commentId, string memberId, CancellationToken ct = default)
    {
        _ = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        await comments.RemoveLikeAsync(commentId, memberId, ct);

        logger.LogInformation("Comment {CommentId} unliked by member {MemberId}", commentId, memberId);
    }

    public async Task<CommentReport> ReportCommentAsync(
        string commentId, string memberId, CancellationToken ct = default)
    {
        _ = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        // Check for duplicate report
        var existing = await comments.GetReportAsync(commentId, memberId, ct);
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var report = new CommentReport
        {
            Id = ObjectIdGenerator.New(),
            CommentId = commentId,
            MemberId = memberId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await comments.AddReportAsync(report, ct);

        logger.LogInformation("Comment {CommentId} reported by member {MemberId}", commentId, memberId);

        return report;
    }

    public async Task HideCommentAsync(string commentId, CancellationToken ct = default)
    {
        var comment = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        comment.Status = "hidden";
        comment.UpdatedAt = DateTime.UtcNow;

        await comments.UpdateAsync(comment, ct);

        logger.LogInformation("Comment {CommentId} hidden by admin", commentId);
    }

    public async Task ApproveCommentAsync(string commentId, CancellationToken ct = default)
    {
        var comment = await comments.GetByIdAsync(commentId, ct)
            ?? throw new InvalidOperationException("Comment not found.");

        comment.Status = "published";
        comment.UpdatedAt = DateTime.UtcNow;

        await comments.UpdateAsync(comment, ct);

        logger.LogInformation("Comment {CommentId} approved by admin", commentId);
    }

    public Task<PagedResult<Comment>> GetCommentsForPostAsync(
        string postId, int page, int pageSize, CancellationToken ct = default)
    {
        return comments.GetByPostIdAsync(postId, page, pageSize, ct);
    }

    public Task<List<Comment>> GetRepliesAsync(string parentId, CancellationToken ct = default)
    {
        return comments.GetRepliesAsync(parentId, ct);
    }

    public Task<int> GetCommentCountAsync(string postId, CancellationToken ct = default)
    {
        return comments.GetCountByPostIdAsync(postId, ct);
    }

    public Task<PagedResult<Comment>> GetAllCommentsAsync(
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        return comments.GetAllAsync(status, page, pageSize, ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
