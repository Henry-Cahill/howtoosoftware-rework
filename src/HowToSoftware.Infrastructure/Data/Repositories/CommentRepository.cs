using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Comments
            .Include(c => c.Member)
            .Include(c => c.Likes)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<PagedResult<Comment>> GetByPostIdAsync(string postId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Comments
            .Include(c => c.Member)
            .Include(c => c.Likes)
            .Where(c => c.PostId == postId && c.ParentId == null && c.Status == "published")
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Comment>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<Comment>> GetAllAsync(string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Comments
            .Include(c => c.Member)
            .Include(c => c.Post)
            .Include(c => c.Likes)
            .Include(c => c.Reports)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Comment>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<Comment>> GetRepliesAsync(string parentId, CancellationToken ct = default)
    {
        return await db.Comments
            .Include(c => c.Member)
            .Include(c => c.Likes)
            .Where(c => c.ParentId == parentId && c.Status == "published")
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountByPostIdAsync(string postId, CancellationToken ct = default)
    {
        return await db.Comments
            .Where(c => c.PostId == postId && c.Status == "published")
            .CountAsync(ct);
    }

    public async Task AddAsync(Comment comment, CancellationToken ct = default)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Comment comment, CancellationToken ct = default)
    {
        db.Comments.Update(comment);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Comments.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
    }

    // Likes

    public async Task<CommentLike?> GetLikeAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        return await db.CommentLikes
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.MemberId == memberId, ct);
    }

    public async Task AddLikeAsync(CommentLike like, CancellationToken ct = default)
    {
        db.CommentLikes.Add(like);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveLikeAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        await db.CommentLikes
            .Where(l => l.CommentId == commentId && l.MemberId == memberId)
            .ExecuteDeleteAsync(ct);
    }

    // Reports

    public async Task<CommentReport?> GetReportAsync(string commentId, string memberId, CancellationToken ct = default)
    {
        return await db.CommentReports
            .FirstOrDefaultAsync(r => r.CommentId == commentId && r.MemberId == memberId, ct);
    }

    public async Task AddReportAsync(CommentReport report, CancellationToken ct = default)
    {
        db.CommentReports.Add(report);
        await db.SaveChangesAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
