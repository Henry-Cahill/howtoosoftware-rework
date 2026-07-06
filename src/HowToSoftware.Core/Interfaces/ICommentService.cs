using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ICommentService
{
    Task<Comment> AddCommentAsync(string postId, string memberId, string html, string? parentId = null, CancellationToken ct = default);
    Task<Comment> EditCommentAsync(string commentId, string memberId, string html, CancellationToken ct = default);
    Task DeleteCommentAsync(string commentId, string memberId, CancellationToken ct = default);
    Task<CommentLike> LikeCommentAsync(string commentId, string memberId, CancellationToken ct = default);
    Task UnlikeCommentAsync(string commentId, string memberId, CancellationToken ct = default);
    Task<CommentReport> ReportCommentAsync(string commentId, string memberId, CancellationToken ct = default);

    // Admin moderation
    Task HideCommentAsync(string commentId, CancellationToken ct = default);
    Task ApproveCommentAsync(string commentId, CancellationToken ct = default);

    // Queries
    Task<PagedResult<Comment>> GetCommentsForPostAsync(string postId, int page, int pageSize, CancellationToken ct = default);
    Task<List<Comment>> GetRepliesAsync(string parentId, CancellationToken ct = default);
    Task<int> GetCommentCountAsync(string postId, CancellationToken ct = default);
    Task<PagedResult<Comment>> GetAllCommentsAsync(string? status, int page, int pageSize, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
