using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<User?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<List<User>> GetAllStaffAsync(CancellationToken ct = default);
    Task<User?> GetByIdWithRolesAsync(string id, CancellationToken ct = default);
    Task<List<User>> GetActiveAuthorsAsync(CancellationToken ct = default);
    Task<PagedResult<User>> GetActiveAuthorsPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> GetPostCountAsync(string userId, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetPostCountsAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
