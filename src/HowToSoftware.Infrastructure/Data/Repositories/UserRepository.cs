using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Slug == slug, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Users.OrderBy(u => u.Name).ToListAsync(ct);
    }

    public async Task<List<User>> GetAllStaffAsync(CancellationToken ct = default)
    {
        return await db.Users
            .Include(u => u.RolesUsers)
                .ThenInclude(ru => ru.Role)
            .Where(u => u.RolesUsers.Any())
            .OrderBy(u => u.Name)
            .ToListAsync(ct);
    }

    public async Task<User?> GetByIdWithRolesAsync(string id, CancellationToken ct = default)
    {
        return await db.Users
            .Include(u => u.RolesUsers)
                .ThenInclude(ru => ru.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<List<User>> GetActiveAuthorsAsync(CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => u.Status == "active"
                && u.PostsAuthors.Any(pa => pa.Post.Status == "published"))
            .OrderBy(u => u.Name)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<User>> GetActiveAuthorsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users
            .Where(u => u.Status == "active"
                && u.PostsAuthors.Any(pa => pa.Post.Status == "published"))
            .OrderBy(u => u.Name);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<User>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<int> GetPostCountAsync(string userId, CancellationToken ct = default)
    {
        return await db.PostsAuthors
            .Where(pa => pa.AuthorId == userId
                && pa.Post.Status == "published"
                && pa.Post.Type == "post")
            .CountAsync(ct);
    }

    public async Task<Dictionary<string, int>> GetPostCountsAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        return await db.PostsAuthors
            .Where(pa => userIds.Contains(pa.AuthorId)
                && pa.Post.Status == "published"
                && pa.Post.Type == "post")
            .GroupBy(pa => pa.AuthorId)
            .Select(g => new { AuthorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AuthorId, x => x.Count, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
