using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Tests.Fakes;

internal class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Slug == slug));

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task<List<User>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Users.ToList());

    public Task<List<User>> GetAllStaffAsync(CancellationToken ct = default)
        => Task.FromResult(Users.Where(u => u.Status == "active").ToList());

    public Task<User?> GetByIdWithRolesAsync(string id, CancellationToken ct = default)
        => GetByIdAsync(id, ct);

    public Task<List<User>> GetActiveAuthorsAsync(CancellationToken ct = default)
        => Task.FromResult(Users.Where(u => u.Status == "active").ToList());

    public Task<PagedResult<User>> GetActiveAuthorsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var active = Users.Where(u => u.Status == "active").ToList();
        var items = active.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<User>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = active.Count,
        });
    }

    public Task<int> GetPostCountAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == userId)?.PostsAuthors.Count ?? 0);

    public Task<Dictionary<string, int>> GetPostCountsAsync(IEnumerable<string> userIds, CancellationToken ct = default)
        => Task.FromResult(userIds.ToDictionary(id => id, id =>
            Users.FirstOrDefault(u => u.Id == id)?.PostsAuthors.Count ?? 0));

    public Task AddAsync(User user, CancellationToken ct = default)
    { Users.Add(user); return Task.CompletedTask; }

    public Task UpdateAsync(User user, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken ct = default)
    { Users.RemoveAll(u => u.Id == id); return Task.CompletedTask; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
