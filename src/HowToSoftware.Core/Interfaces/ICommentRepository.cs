using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PagedResult<Comment>> GetByPostIdAsync(string postId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Comment>> GetAllAsync(string? status, int page, int pageSize, CancellationToken ct = default);
    Task<List<Comment>> GetRepliesAsync(string parentId, CancellationToken ct = default);
    Task<int> GetCountByPostIdAsync(string postId, CancellationToken ct = default);
    Task AddAsync(Comment comment, CancellationToken ct = default);
    Task UpdateAsync(Comment comment, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);

    // Likes
    Task<CommentLike?> GetLikeAsync(string commentId, string memberId, CancellationToken ct = default);
    Task AddLikeAsync(CommentLike like, CancellationToken ct = default);
    Task RemoveLikeAsync(string commentId, string memberId, CancellationToken ct = default);

    // Reports
    Task<CommentReport?> GetReportAsync(string commentId, string memberId, CancellationToken ct = default);
    Task AddReportAsync(CommentReport report, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
